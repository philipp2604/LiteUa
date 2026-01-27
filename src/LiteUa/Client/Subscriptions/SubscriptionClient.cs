using LiteUa.BuiltIn;
using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Stack.Subscription;
using LiteUa.Transport;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Client.Subscriptions
{

    /// <summary>
    /// Client for managing subscriptions to an OPC UA server, allowing for monitoring of data changes on specified nodes.
    /// </summary>
    public class SubscriptionClient : ISubscriptionClient
    {
        // Configuration
        private readonly string _endpointUrl;

        private readonly string _applicationUri;
        private readonly string _productUri;
        private readonly string _applicationName;
        private readonly IUserIdentity _userIdentity;
        private readonly ISecurityPolicyFactory _securityPolicyFactory;
        private readonly X509Certificate2? _clientCert;
        private readonly X509Certificate2? _serverCert;
        private readonly MessageSecurityMode _securityMode;
        private readonly IUaTcpClientChannelFactory _clientChannelFactory;
        private readonly int _supervisorIntervalMs;
        private readonly int _reconnectIntervalMs;
        private readonly uint _heartbeatIntervalMs;
        private readonly uint _heartbeatTimeoutHintMs;
        private readonly uint _maxPublishRequests;
        private readonly double _publishTimeoutMultiplier;
        private readonly uint _minPublishTimeout;

        // Runtime State
        private IUaTcpClientChannel? _channel;

        // Mapping: PublishingInterval -> Bucket
        private readonly ConcurrentDictionary<double, SubscriptionBucket> _buckets = new();

        // Handles
        private uint _nextClientHandle = 1;

        private readonly Lock _handleLock = new();

        // Lifecycle
        private readonly TaskCompletionSource _firstConnectionTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private CancellationTokenSource? _lifecycleCts;
        private Task? _reconnectTask;
        private volatile bool _isConnected = false;
        private readonly Lock _reconnectLock = new();
        private bool _isReconnecting = false;

        // Events
        /// <summary>
        /// A callback that is invoked when data changes for a subscribed node.
        /// </summary>
        public event Action<uint, DataValue>? DataChanged;

        /// <summary>
        /// A callback that is invoked when the connection status changes.
        /// </summary>
        public event Action<bool>? ConnectionStatusChanged;

        /// <summary>
        /// Creates a new instance of the <see cref="SubscriptionClient"/> class.
        /// </summary>
        /// <param name="endpointUrl">The server's endpoint url including protocol and port, e.g. 'opc.tcp://192.178.0.1:4840/'.</param>
        /// <param name="applicationUri">The application uri.</param>
        /// <param name="productUri">The product uri.</param>
        /// <param name="applicationName">The application name.</param>
        /// <param name="userIdentity">The user identity of type <see cref="IUserIdentity"/> to use.</param>
        /// <param name="policyFactory">An instance of <see cref="ISecurityPolicy"/>.</param>
        /// <param name="securityMode">The message security mode to use.</param>
        /// <param name="clientCertificate">Optional: The client's certificate.</param>
        /// <param name="serverCertificate">Optional: The server's certificate.</param>
        /// <param name="heartbeatIntervalMs"">The heartbeat interval in milliseconds.</param>
        /// <param name="heartbeatTimeoutHintMs">The heartbeat timeout hint in milliseconds.</param>
        /// <param name="maxPublishRequests">The maximum number of concurrent publish requests.</param>
        /// <param name="publishTimeoutMultiplier">The multiplier for calculating publish timeouts.</param>
        /// <param name="minPublishTimeout">The minimum publish timeout in milliseconds.</param>
        /// <param name="clientChannelFactory">An instance of <see cref="IUaTcpClientChannelFactory"/>.</param>
        /// <param name="supervisorIntervalMs">The interval in milliseconds for the supervisor loop to check connection status.</param>
        /// <param name="reconnectIntervalMs">The interval in milliseconds to wait before attempting to reconnect after a disconnection.</param>
        public SubscriptionClient(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            IUserIdentity userIdentity,
            ISecurityPolicyFactory policyFactory,
            MessageSecurityMode securityMode,
            X509Certificate2? clientCertificate,
            X509Certificate2? serverCertificate,
            uint heartbeatIntervalMs,
            uint heartbeatTimeoutHintMs,
            uint maxPublishRequests,
            double publishTimeoutMultiplier,
            uint minPublishTimeout,
            IUaTcpClientChannelFactory clientChannelFactory,
            int supervisorIntervalMs = 1000,
            int reconnectIntervalMs = 5000)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpointUrl);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(productUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationName);
            ArgumentNullException.ThrowIfNull(userIdentity);
            ArgumentNullException.ThrowIfNull(policyFactory);
            ArgumentNullException.ThrowIfNull(clientChannelFactory);

            _endpointUrl = endpointUrl;
            _applicationUri = applicationUri;
            _productUri = productUri;
            _applicationName = applicationName;
            _userIdentity = userIdentity;
            _securityPolicyFactory = policyFactory;
            _securityMode = securityMode;
            _clientCert = clientCertificate;
            _serverCert = serverCertificate;
            _heartbeatIntervalMs = heartbeatIntervalMs;
            _heartbeatTimeoutHintMs = heartbeatTimeoutHintMs;
            _clientChannelFactory = clientChannelFactory;
            _reconnectIntervalMs = reconnectIntervalMs;
            _supervisorIntervalMs = supervisorIntervalMs;
            _maxPublishRequests = maxPublishRequests;
            _publishTimeoutMultiplier = publishTimeoutMultiplier;
            _minPublishTimeout = minPublishTimeout;
        }

        public void Start()
        {
            if (_lifecycleCts != null) return;
            _lifecycleCts = new CancellationTokenSource();
            _reconnectTask = Task.Run(SupervisorLoop);
        }

        public async Task<uint[]> SubscribeAsync(NodeId[] nodeIds, double publishingInterval = 1000.0)
        {
            await _firstConnectionTcs.Task;

            uint[] handles = new uint[nodeIds.Length];

            lock (_handleLock)
            {
                for (int i = 0; i < handles.Length; i++)
                    handles[i] = _nextClientHandle++;
            }

            var bucket = _buckets.GetOrAdd(publishingInterval, interval => new SubscriptionBucket(interval, _maxPublishRequests, _publishTimeoutMultiplier, _minPublishTimeout));

            for (int i = 0; i < nodeIds.Length; i++)
            {
                await bucket.AddItemAsync(handles[i], nodeIds[i]);
            }

            if (_isConnected && _channel != null)
            {
                try
                {
                    await bucket.EnsureSubscriptionCreatedAsync(_channel, OnDataChanged, OnSubscriptionError);
                    if (bucket.LiveSubscription != null)
                        await bucket.LiveSubscription.CreateMonitoredItemsAsync(nodeIds, handles);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            return handles;
        }

        private void OnDataChanged(uint handle, DataValue value) => DataChanged?.Invoke(handle, value);

        private void OnSubscriptionError(Exception ex)
        {
            TriggerReconnect();
        }

        private void TriggerReconnect()
        {
            lock (_reconnectLock)
            {
                if (_isReconnecting) return;
                _isConnected = false;
                _isReconnecting = true;
            }
            ConnectionStatusChanged?.Invoke(false);
        }

        private async Task SupervisorLoop()
        {
            while (_lifecycleCts != null && !_lifecycleCts.IsCancellationRequested)
            {
                if (!_isConnected)
                {
                    try
                    {
                        await ConnectAndRestoreAsync();
                        lock (_reconnectLock) { _isConnected = true; _isReconnecting = false; }
                        _firstConnectionTcs.TrySetResult();
                        ConnectionStatusChanged?.Invoke(true);
                    }
                    catch (Exception)
                    {
                        if (!_lifecycleCts.IsCancellationRequested)
                        {
                            await Task.Delay(_reconnectIntervalMs, _lifecycleCts.Token);
                        }
                    }
                }
                else
                {
                    try { await Task.Delay(_supervisorIntervalMs, _lifecycleCts.Token); } catch { }
                }
            }
        }

        private async Task ConnectAndRestoreAsync()
        {
            // close old channel
            if (_channel != null)
            {
                try { await _channel.DisposeAsync(); } catch { }
                _channel = null;
            }

            // reset Buckets
            foreach (var bucket in _buckets.Values) bucket.ClearLiveReference();

            // create new channel
            _channel = _clientChannelFactory.CreateTcpClientChannel(_endpointUrl, _applicationUri, _productUri, _applicationName, _securityPolicyFactory, _securityMode, _clientCert, _serverCert, _heartbeatIntervalMs, _heartbeatTimeoutHintMs);
            await _channel.ConnectAsync();
            await _channel.CreateSessionAsync("SubscriptionMonitoringSession");
            await _channel.ActivateSessionAsync(_userIdentity);

            // 3. Restore
            foreach (var bucket in _buckets.Values)
            {
                await bucket.EnsureSubscriptionCreatedAsync(_channel, OnDataChanged, OnSubscriptionError);
                await bucket.RestoreItemsAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            _lifecycleCts?.Cancel();

            foreach (var bucket in _buckets.Values)
            {
                await bucket.DisposeAsync();
            }

            if (_channel != null)
            {
                await _channel.DisposeAsync();
            }

            _lifecycleCts?.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
            GC.SuppressFinalize(this);
        }

        private class SubscriptionBucket(double interval, uint maxPublishRequests, double publishTimeoutMultiplier, uint minPublishTimeout) : IAsyncDisposable
        {
            public double Interval { get; } = interval; public Subscription? LiveSubscription { get; private set; }
            private readonly Dictionary<uint, NodeId> _items = [];
            private readonly SemaphoreSlim _asyncLock = new(1, 1);
            private readonly uint _maxPublishRequests = maxPublishRequests;
            private readonly double _publishTimeoutMultiplier = publishTimeoutMultiplier;
            private readonly uint _minPublishTimeout = minPublishTimeout;

            public void ClearLiveReference()
            {
                LiveSubscription = null;
            }

            public async Task EnsureSubscriptionCreatedAsync(
                IUaTcpClientChannel channel,
                Action<uint, DataValue> cb,
                Action<Exception> errCb)
            {
                if (LiveSubscription != null) return;
                var sub = new Subscription(channel, _maxPublishRequests, _publishTimeoutMultiplier, _minPublishTimeout);
                sub.DataChanged += cb;
                sub.ConnectionLost += errCb;
                await sub.CreateAsync(Interval);
                LiveSubscription = sub;
            }

            public async Task AddItemAsync(uint handle, NodeId nodeId)
            {
                await _asyncLock.WaitAsync();
                try
                {
                    _items[handle] = nodeId;
                }
                finally
                {
                    _asyncLock.Release();
                }
            }

            public async Task RestoreItemsAsync()
            {
                if (LiveSubscription == null) return;

                await _asyncLock.WaitAsync();
                try
                {
                    if (_items.Count == 0) return;

                    var nodeIds = _items.Values.ToArray();
                    var handles = _items.Keys.ToArray();

                    await LiveSubscription.CreateMonitoredItemsAsync(nodeIds, handles);
                }
                finally
                {
                    _asyncLock.Release();
                }
            }

            public async ValueTask DisposeAsync()
            {
                await _asyncLock.WaitAsync();
                try
                {
                    if (LiveSubscription != null)
                    {
                        await LiveSubscription.DisposeAsync();
                        LiveSubscription = null;
                    }
                }
                finally
                {
                    _asyncLock.Release();
                    _asyncLock.Dispose();
                }
            }
        }
    }
}