using LiteUa.BuiltIn;
using LiteUa.Client.Building;
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
    /// <summary>
    /// An orchestrating OPC UA Client.
    /// </summary>
    public class UaClient : IUaClient
    {
        private readonly UaClientOptions _options;
        private readonly ISecurityPolicyFactory _policyFactory;
        private readonly IUaTcpClientChannelFactory _tcpClientChannelFactory;
        private readonly IUaInnerClientsFactory _innerClientsFactory;

        // Subscription Callbacks
        private readonly ConcurrentDictionary<uint, Action<uint, DataValue>> _subscriptionCallbacks = new();

        internal IUaClientPool? _pool;
        internal ISubscriptionClient? _subscriptionClient;

        /// <summary>
        /// A callback that is invoked when the connection status changes.
        /// Returns true if connected, false if disconnected.
        /// </summary>
        public event Action<bool>? ConnectionStatusChanged;

        /// <summary>
        /// Creates a new instance of the <see cref="UaClient"/> class with the specified options, ChannelFactory, and InnerClientsFactory.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="channelFactory"></param>
        /// <param name="innerClientsFactory"></param>
        /// <exception cref="NotImplementedException"></exception>
        public UaClient(UaClientOptions options, IUaTcpClientChannelFactory channelFactory, IUaInnerClientsFactory innerClientsFactory)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(channelFactory);
            ArgumentNullException.ThrowIfNull(innerClientsFactory);

            _options = options;
            _tcpClientChannelFactory = channelFactory;
            _innerClientsFactory = innerClientsFactory;

            _policyFactory = options.Security.PolicyType switch
            {
                SecurityPolicyType.None => new SecurityPolicyFactoryNone(),
                SecurityPolicyType.Basic256Sha256 => new SecurityPolicyFactoryBasic256Sha256(),
                _ => throw new NotImplementedException("SecurityPolicyType not implemented."),
            };
        }

        /// <summary>
        /// Builder entry point for creating an UaClient.
        /// </summary>
        /// <returns>A <see cref="UaClientBuilder"/>.</returns>
        public static UaClientBuilder Create() => new();

        /// <summary>
        /// Connects the client to the server, performing discovery, security setup, and session establishment.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the async operations.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            // 1. Discovery
            var discoveryClient = _innerClientsFactory.CreateDiscoveryClient(
                _options.EndpointUrl,
                _options.Session.ApplicationUri,
                _options.Session.ProductUri,
                _options.Session.ApplicationName,
                _options.Limits.HeartbeatIntervalMs,
                _options.Limits.HeartbeatTimeoutHintMs,
            _tcpClientChannelFactory);

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
            _subscriptionClient = _innerClientsFactory.CreateSubscriptionClient(
                _options.EndpointUrl,
                _options.Session.ApplicationUri,
                _options.Session.ProductUri,
                _options.Session.ApplicationName,
                identity,
                _policyFactory,
                _options.Security.MessageSecurityMode,
                _options.Security.ClientCertificate,
                _options.Security.ServerCertificate,
                _options.Limits.HeartbeatIntervalMs,
                _options.Limits.HeartbeatTimeoutHintMs,
                _options.Limits.MaxPublishRequestCount,
                _options.Limits.PublishTimeoutMultiplier,
                _options.Limits.MinPublishTimeoutMs,
                _tcpClientChannelFactory,
                _options.Limits.SupervisorIntervalMs,
                _options.Limits.ReconnectIntervalMs
            );

            _subscriptionClient.DataChanged += OnSubscriptionDataChanged;
            _subscriptionClient.ConnectionStatusChanged += (connected) =>
            {
                if (!connected)
                {
                    _pool?.Clear();
                }
                ConnectionStatusChanged?.Invoke(connected);
            };
            _subscriptionClient.Start();

            // 2. Setup Pool (Request/Response)
            _pool = _innerClientsFactory.CreateUaClientPool(
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
                _options.Limits.HeartbeatIntervalMs,
                _options.Limits.HeartbeatTimeoutHintMs,
                _tcpClientChannelFactory
            );

        }

        #region Read / Write / Browse (via Pool)

        /// <summary>
        /// Reads the specified nodes asynchronously.
        /// </summary>
        /// <param name="nodeIds">The nodes to read.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the async operations.</param>
        /// <returns>A task encapsulating the read DataValues.</returns>
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

        /// <summary>
        /// Writes the specified values to the specified nodes asynchronously.
        /// </summary>
        /// <param name="nodeIds">The nodes to write to.</param>
        /// <param name="values">The values to write to the nodes.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operations.</param>
        /// <returns>A task encapsulating the returned StatusCodes.</returns>
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

        /// <summary>
        /// Browses the specified nodes asynchronously.
        /// </summary>
        /// <param name="nodeIds">The nodes to browse.</param>
        /// <param name="maxRefs">Maxmium reference descriptions to return.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operations.</param>
        /// <returns>A task encapsulating the returned ReferenceDescriptions.</returns>
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

        /// <summary>
        /// Calls a method on the server with typed input and output arguments.
        /// </summary>
        /// <typeparam name="TInput">Input arguments type.</typeparam>
        /// <typeparam name="TOutput">Output type.</typeparam>
        /// <param name="objectId">Node Id of the method object.</param>
        /// <param name="methodId">Node Id of the method.</param>
        /// <param name="inputArgs">Input arguments of type <typeparamref name="TInput"/>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operations.</param>
        /// <returns>A task encapsulating the method's output of type <typeparamref name="TOutput"/></returns>
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

        /// <summary>
        /// Calls a method on the server with untyped input and output arguments.
        /// </summary>
        /// <param name="objectId">Node Id of the method object.</param>
        /// <param name="methodId">Node Id of the method.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operations.</param>
        /// <param name="inputArguments">Input arguments for the method call.</param>
        /// <returns>A task encapsulating the method's output as <see cref="Variant"/> array.</returns>
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

        /// <summary>
        /// Subscribes to data changes for the specified node IDs with a single callback.
        /// </summary>
        /// <param name="nodeIds">NodeIds to subscribe to.</param>
        /// <param name="callback">Callback to call when a data change occured.</param>
        /// <param name="interval">Sampling interval.</param>
        /// <returns>A task encapsulating an array of the subscription handles.</returns>
        /// <exception cref="InvalidOperationException"></exception>
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
        /// <summary>
        /// Subscribes to data changes for the specified node IDs with individual callbacks.
        /// </summary>
        /// <param name="nodeIds">NodeIds to subscribe to.</param>
        /// <param name="callbacks">Caöllbacks for every NodeId that are being called after a data change.</param>
        /// <param name="interval">Sampling interval.</param>
        /// <returns>A task encapsulating an array of the subscription handles.</returns>
        /// <exception cref="InvalidOperationException"></exception>
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