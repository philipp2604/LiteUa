using LiteUa.BuiltIn;
using LiteUa.Client.Pooling;
using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Stack.View;
using LiteUa.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Client
{
    public class UaClient(UaClientOptions options) : IDisposable, IAsyncDisposable
    {
        private readonly UaClientOptions _options = options;
        private UaClientPool? _pool;
        private SubscriptionClient? _subscriptionClient;

        // Subscription Callbacks
        private readonly ConcurrentDictionary<uint, Action<DataValue>> _subscriptionCallbacks = new();

        // Builder entry point
        public static UaClientBuilder Create() => new();

        public async Task Connect()
        {
            // 1. Discovery
            var discoveryClient = new DiscoveryClient(
                _options.EndpointUrl,
                _options.Session.ApplicationUri,
                _options.Session.ProductUri,
                _options.Session.ApplicationName);

            string policyUri = _options.Security.PolicyType switch
            {
                SecurityPolicyType.None => SecurityPolicyUris.None,
                SecurityPolicyType.Basic256Sha256 => SecurityPolicyUris.Basic256Sha256,
                _ => throw new NotImplementedException("Security policy not implemented."),
            };

            var endpoint = await discoveryClient.GetEndpoint(
                _options.Security.MessageSecurityMode,
                policyUri,
                _options.Security.UserTokenType
            ) ?? throw new InvalidDataException("Could not find suitable endpoint.");

            var serverToken = endpoint.UserIdentityTokens!.FirstOrDefault(t => t.TokenType == (int)_options.Security.UserTokenType && t.SecurityPolicyUri == (_options.Security.UserTokenType == UserTokenType.Anonymous ? null : policyUri));

            _options.Security.ServerCertificate = endpoint.ServerCertificate != null ? X509CertificateLoader.LoadCertificate(endpoint.ServerCertificate) : null;

            ISecurityPolicy policy = _options.Security.PolicyType switch
            {
                SecurityPolicyType.None => new SecurityPolicyNone(),
                SecurityPolicyType.Basic256Sha256 => new SecurityPolicyBasic256Sha256(_options.Security.ClientCertificate!, _options.Security.ServerCertificate!),
                _ => throw new NotImplementedException("Security policy not implemented."),
            };

            IUserIdentity identity = _options.Security.UserTokenType switch
            {
                UserTokenType.Anonymous => new AnonymousIdentity(),
                UserTokenType.Username => new UserNameIdentity(serverToken!.PolicyId!, _options.Security.Username!, _options.Security.Password!),
                _ => throw new NotImplementedException("Security policy not implemented."),
            };

            // 1. Setup Subscription Client
            _subscriptionClient = new SubscriptionClient(
                _options.EndpointUrl,
                _options.Session.ApplicationUri,
                _options.Session.ProductUri,
                _options.Session.ApplicationName,
                identity,
                policy,
                _options.Security.MessageSecurityMode,
                _options.Security.ClientCertificate,
                _options.Security.ServerCertificate
            );

            _subscriptionClient.DataChanged += OnSubscriptionDataChanged;
            _subscriptionClient.Start();

            // 2. Setup Pool (Request/Response)
            _pool = new UaClientPool(
                _options.EndpointUrl,
                _options.Session.ApplicationUri,
                _options.Session.ProductUri,
                _options.Session.ApplicationName,
                policy,
                identity,
                _options.Security.MessageSecurityMode,
                _options.Security.ClientCertificate,
                _options.Security.ServerCertificate,
                _options.Pool.MaxSize
            );
        }

        #region Read / Write / Browse (via Pool)

        public async Task<DataValue[]?> ReadNodesAsync(NodeId[] nodeIds)
        {
            EnsureConnected();
            using var pooled = await _pool!.RentAsync();
            try
            {
                var results = await pooled.InnerClient.ReadAsync(nodeIds);
                if (results == null || results.Length == 0) throw new Exception("Read returned no results");
                return results;
            }
            catch (Exception)
            {
                pooled.IsInvalid = true; // Mark as broken so pool disposes it
                throw;
            }
        }

        public async Task WriteNodesAsync(NodeId[] nodeIds, DataValue[] values)
        {
            EnsureConnected();
            using var pooled = await _pool!.RentAsync();
            try
            {
                var results = await pooled.InnerClient.WriteAsync(nodeIds, values);
                if (results != null && results.Length > 0 && results.Any(sc => !sc.IsGood))
                {
                    throw new Exception($"Write failed with status: {results}");
                }
            }
            catch (Exception)
            {
                pooled.IsInvalid = true;
                throw;
            }
        }

        public async Task<ReferenceDescription[][]?> BrowseNodesAsync(NodeId[] nodeIds)
        {
            EnsureConnected();
            using var pooled = await _pool!.RentAsync();
            try
            {
                var results = await pooled.InnerClient.BrowseAsync(nodeIds);
                return results;
            }
            catch (Exception)
            {
                pooled.IsInvalid = true;
                throw;
            }
        }

        /// TODO: Implement CallMethodsAsync -> bulk call
        public async Task<TOutput> CallMethodAsync<TInput, TOutput>(NodeId objectId, NodeId methodId, TInput inputArgs)
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
                    inputArgs
                );
            }
            catch
            {
                pooled.IsInvalid = true;
                throw;
            }
        }

        #endregion

        #region Subscription (via SubscriptionClient)

        /// TODO: Implement SubscribeAsync -> bulk subscribe
        public async Task<uint> SubscribeAsync(NodeId nodeId, Action<DataValue> callback, double interval = 1000.0)
        {
            if (_subscriptionClient == null) throw new InvalidOperationException("Client not connected. Call Connect() first.");

            var handle = await _subscriptionClient.SubscribeAsync(nodeId, interval);
            _subscriptionCallbacks.TryAdd(handle, callback);
            return handle;
        }

        private void OnSubscriptionDataChanged(uint handle, DataValue value)
        {
            if (_subscriptionCallbacks.TryGetValue(handle, out var cb))
            {
                cb.Invoke(value);
            }
        }

        #endregion

        private void EnsureConnected()
        {
            if (_pool == null) throw new InvalidOperationException("Client not connected. Call Connect() first.");
        }

        public async ValueTask DisposeAsync()
        {
            if (_pool != null) await _pool.DisposeAsync();
            if (_subscriptionClient != null) await _subscriptionClient.DisposeAsync();
            _subscriptionCallbacks.Clear();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            _pool?.Dispose();
            _subscriptionClient?.Dispose();
            _subscriptionCallbacks.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
