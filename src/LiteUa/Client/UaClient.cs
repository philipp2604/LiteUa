using LiteUa.BuiltIn;
using LiteUa.Client.Building;
using LiteUa.Client.Discovery;
using LiteUa.Client.Pooling;
using LiteUa.Client.Subscriptions;
using LiteUa.Security.Policies;
using LiteUa.Stack.Session.Identity;
using LiteUa.Stack.View;
using LiteUa.Transport;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Client
{
    public class UaClient : IDisposable, IAsyncDisposable
    {
        private readonly UaClientOptions _options;
        private readonly ISecurityPolicyFactory _policyFactory;
        private UaClientPool? _pool;
        private SubscriptionClient? _subscriptionClient;

        // Subscription Callbacks
        private readonly ConcurrentDictionary<uint, Action<uint, DataValue>> _subscriptionCallbacks = new();

        public UaClient(UaClientOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options;

            _policyFactory = options.Security.PolicyType switch
            {
                SecurityPolicyType.None => new SecurityPolicyFactoryNone(),
                SecurityPolicyType.Basic256Sha256 => new SecurityPolicyFactoryBasic256Sha256(),
                _ => throw new NotImplementedException("SecurityPolicyType not implemented."),
            };
        }

        // Builder entry point
        public static UaClientBuilder Create() => new();

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            // 1. Discovery
            var discoveryClient = new DiscoveryClient(
                _options.EndpointUrl,
                _options.Session.ApplicationUri,
                _options.Session.ProductUri,
                _options.Session.ApplicationName,
            new UaTcpClientChannelFactory());

            string policyUri = _options.Security.PolicyType switch
            {
                SecurityPolicyType.None => SecurityPolicyUris.None,
                SecurityPolicyType.Basic256Sha256 => SecurityPolicyUris.Basic256Sha256,
                _ => throw new NotImplementedException("Security policy not implemented."),
            };

            var endpoint = await discoveryClient.GetEndpoint(
                _options.Security.MessageSecurityMode,
                policyUri,
                _options.Security.UserTokenType,
                cancellationToken
            ) ?? throw new InvalidDataException("Could not find suitable endpoint.");

            var userTokenPolicyUri = _options.Security.UserTokenPolicyType switch
            {
                SecurityPolicyType.None => SecurityPolicyUris.None,
                SecurityPolicyType.Basic256Sha256 => SecurityPolicyUris.Basic256Sha256,
                _ => throw new NotImplementedException("Security policy not implemented")
            };

            var serverToken = endpoint.UserIdentityTokens!.FirstOrDefault(t => t.TokenType == (int)_options.Security.UserTokenType && t.SecurityPolicyUri == (_options.Security.UserTokenType == UserTokenType.Anonymous ? null : userTokenPolicyUri));

            _options.Security.ServerCertificate = endpoint.ServerCertificate != null ? X509CertificateLoader.LoadCertificate(endpoint.ServerCertificate) : null;

            ISecurityPolicy policy = _options.Security.PolicyType switch
            {
                SecurityPolicyType.None => new SecurityPolicyNone(),
                SecurityPolicyType.Basic256Sha256 => new SecurityPolicyBasic256Sha256(_options.Security.ClientCertificate!, _options.Security.ServerCertificate!),
                _ => throw new NotImplementedException("Security policy not implemented."),
            };

            IUserIdentity identity = _options.Security.UserTokenType switch
            {
                UserTokenType.Anonymous => new AnonymousIdentityToken(),
                UserTokenType.Username => new UserNameIdentityToken(serverToken!.PolicyId!, _options.Security.Username!, _options.Security.Password!),
                _ => throw new NotImplementedException("Security policy not implemented."),
            };

            // 1. Setup Subscription Client
            _subscriptionClient = new SubscriptionClient(
                _options.EndpointUrl,
                _options.Session.ApplicationUri,
                _options.Session.ProductUri,
                _options.Session.ApplicationName,
                identity,
                _policyFactory,
                _options.Security.MessageSecurityMode,
                _options.Security.ClientCertificate,
                _options.Security.ServerCertificate,
                new UaTcpClientChannelFactory()
            );

            _subscriptionClient.DataChanged += OnSubscriptionDataChanged;
            _subscriptionClient.Start();

            // 2. Setup Pool (Request/Response)
            _pool = new UaClientPool(
                _options.EndpointUrl,
                _options.Session.ApplicationUri,
                _options.Session.ProductUri,
                _options.Session.ApplicationName,
                identity,
                _policyFactory,
                _options.Security.MessageSecurityMode,
                _options.Security.ClientCertificate,
                _options.Security.ServerCertificate,
                _options.Pool.MaxSize,
                new UaTcpClientChannelFactory()
            );
        }

        #region Read / Write / Browse (via Pool)

        public async Task<DataValue[]?> ReadNodesAsync(NodeId[] nodeIds, CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            using var pooled = await _pool!.RentAsync();
            try
            {
                var results = await pooled.InnerClient.ReadAsync(nodeIds, cancellationToken);
                if (results == null || results.Length == 0) throw new Exception("Read returned no results");
                return results;
            }
            catch (Exception)
            {
                pooled.IsInvalid = true; // Mark as broken so pool disposes it
                throw;
            }
        }

        public async Task<StatusCode[]?> WriteNodesAsync(NodeId[] nodeIds, DataValue[] values, CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            using var pooled = await _pool!.RentAsync();
            try
            {
                var results = await pooled.InnerClient.WriteAsync(nodeIds, values, cancellationToken);
                if (results != null && results.Length > 0 && results.Any(sc => !sc.IsGood))
                {
                    throw new Exception($"Write failed with status: {results}");
                }

                return results;
            }
            catch (Exception)
            {
                pooled.IsInvalid = true;
                throw;
            }
        }

        public async Task<ReferenceDescription[][]?> BrowseNodesAsync(NodeId[] nodeIds, uint maxRefs = 0, CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            using var pooled = await _pool!.RentAsync();
            try
            {
                var results = await pooled.InnerClient.BrowseAsync(nodeIds, maxRefs, cancellationToken);
                return results;
            }
            catch (Exception)
            {
                pooled.IsInvalid = true;
                throw;
            }
        }

        public async Task<TOutput> CallMethodAsync<TInput, TOutput>(NodeId objectId, NodeId methodId, TInput inputArgs, CancellationToken cancellationToken = default)
            where TInput : class
            where TOutput : class, new()
        {
            EnsureConnected();
            using var pooled = await _pool!.RentAsync();
            try
            {
                return await pooled.InnerClient.CallTypedAsync<TInput, TOutput>(
                    objectId,
                    methodId,
                    inputArgs,
                    cancellationToken
                );
            }
            catch
            {
                pooled.IsInvalid = true;
                throw;
            }
        }

        public async Task<Variant[]> CallMethodAsync(NodeId objectId, NodeId methodId, CancellationToken cancellationToken = default, params Variant[] inputArguments)
        {
            EnsureConnected();
            using var pooled = await _pool!.RentAsync();
            try
            {
                return await pooled.InnerClient.CallAsync(
                    objectId,
                    methodId,
                    cancellationToken,
                    inputArguments
                );
            }
            catch
            {
                pooled.IsInvalid = true;
                throw;
            }
        }

        #endregion Read / Write / Browse (via Pool)

        #region Subscription (via SubscriptionClient)

        // multiple node Ids, one callback
        public async Task<uint[]> SubscribeAsync(NodeId[] nodeIds, Action<uint, DataValue> callback, double interval = 1000.0)
        {
            if (_subscriptionClient == null) throw new InvalidOperationException("Client not connected. Call Connect() first.");

            var handles = await _subscriptionClient.SubscribeAsync(nodeIds, interval);

            foreach (var handle in handles)
            {
                _subscriptionCallbacks.TryAdd(handle, callback);
            }
            return handles;
        }

        // multiple node Ids, multiple callbacks
        public async Task<uint[]> SubscribeAsync(NodeId[] nodeIds, Action<uint, DataValue>[] callbacks, double interval = 1000.0)
        {
            if (_subscriptionClient == null) throw new InvalidOperationException("Client not connected. Call Connect() first.");

            var handles = await _subscriptionClient.SubscribeAsync(nodeIds, interval);

            for (int i = 0; i < handles.Length; i++)
            {
                _subscriptionCallbacks.TryAdd(handles[i], callbacks[i]);
            }
            return handles;
        }

        private void OnSubscriptionDataChanged(uint handle, DataValue value)
        {
            if (_subscriptionCallbacks.TryGetValue(handle, out var cb))
            {
                cb.Invoke(handle, value);
            }
        }

        #endregion Subscription (via SubscriptionClient)

        private void EnsureConnected()
        {
            if (_pool == null) throw new InvalidOperationException("Client not connected. Call Connect() first.");
        }

        public async ValueTask DisposeAsync()
        {
            if (_pool != null) await _pool.DisposeAsync();
            if (_subscriptionClient != null) await _subscriptionClient.DisposeAsync();
            _subscriptionCallbacks.Clear();
            await _options.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
            GC.SuppressFinalize(this);
        }
    }
}