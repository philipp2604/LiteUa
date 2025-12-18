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
    public class SubscriptionClient : IDisposable, IAsyncDisposable
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

        // Runtime State
        private UaTcpClientChannel? _channel;

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
        public event Action<uint, DataValue>? DataChanged;

        public event Action<bool>? ConnectionStatusChanged;

        public SubscriptionClient(string endpointUrl, string applicationUri, string productUri, string applicationName, IUserIdentity userIdentity, ISecurityPolicyFactory policyFactory, MessageSecurityMode securityMode, X509Certificate2? clientCertificate, X509Certificate2? serverCertificate)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpointUrl);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(productUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationName);
            ArgumentNullException.ThrowIfNull(userIdentity);
            ArgumentNullException.ThrowIfNull(policyFactory);

            _endpointUrl = endpointUrl;
            _applicationUri = applicationUri;
            _productUri = productUri;
            _applicationName = applicationName;
            _userIdentity = userIdentity;
            _securityPolicyFactory = policyFactory;
            _securityMode = securityMode;
            _clientCert = clientCertificate;
            _serverCert = serverCertificate;
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

            var bucket = _buckets.GetOrAdd(publishingInterval, interval => new SubscriptionBucket(interval));

            for (int i = 0; i < nodeIds.Length; i++)
            {
                bucket.AddItem(handles[i], nodeIds[i]);
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

        /*
        public async Task<uint> SubscribeAsync(NodeId nodeId, double publishingInterval = 1000.0)
        {
            await _firstConnectionTcs.Task;

            uint handle;
            lock (_handleLock) handle = _nextClientHandle++;

            var bucket = _buckets.GetOrAdd(publishingInterval, interval => new SubscriptionBucket(interval));
            bucket.AddItem(handle, nodeId);

            if (_isConnected && _channel != null)
            {
                try
                {
                    await bucket.EnsureSubscriptionCreatedAsync(_channel, OnDataChanged, OnSubscriptionError);
                    if(bucket.LiveSubscription != null)
                        await bucket.LiveSubscription.CreateMonitoredItemAsync(nodeId, handle);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            return handle;
        }
        */

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
                            await Task.Delay(5000, _lifecycleCts.Token);
                        }
                    }
                }
                else
                {
                    try { await Task.Delay(1000, _lifecycleCts.Token); } catch { }
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
            _channel = new UaTcpClientChannel(_endpointUrl, _applicationUri, _productUri, _applicationName, _securityPolicyFactory, _securityMode, _clientCert, _serverCert);
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

        // --- DISPOSE  ---

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

        // --- Inner Class ---
        private class SubscriptionBucket(double interval) : IAsyncDisposable
        {
            public double Interval { get; } = interval; public Subscription? LiveSubscription { get; private set; }
            private readonly Dictionary<uint, NodeId> _items = [];
            private readonly Lock _lock = new();

            public void AddItem(uint handle, NodeId nodeId)
            {
                lock (_lock) _items[handle] = nodeId;
            }

            public void ClearLiveReference()
            {
                LiveSubscription = null;
            }

            public async Task EnsureSubscriptionCreatedAsync(
                UaTcpClientChannel channel,
                Action<uint, DataValue> cb,
                Action<Exception> errCb)
            {
                if (LiveSubscription != null) return;
                var sub = new Subscription(channel);
                sub.DataChanged += cb;
                sub.ConnectionLost += errCb;
                await sub.CreateAsync(Interval);
                LiveSubscription = sub;
            }

            public async Task RestoreItemsAsync()
            {
                if (LiveSubscription == null) return;

                lock (_lock)
                {
                    if (_items.Count == 0) return;

                    var nodeIds = _items.Values.ToArray();
                    var handles = _items.Keys.ToArray();
                    try
                    {
                        LiveSubscription.CreateMonitoredItemsAsync(nodeIds, handles).Wait();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }

            public async ValueTask DisposeAsync()
            {
                if (LiveSubscription != null)
                {
                    await LiveSubscription.DisposeAsync();
                    LiveSubscription = null;
                }
            }
        }
    }
}