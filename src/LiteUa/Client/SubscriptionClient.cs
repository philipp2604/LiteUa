using LiteUa.BuiltIn;
using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LiteUa.Client
{
    public class SubscriptionClient(
        string endpointUrl,
        IUserIdentity? userIdentity = null,
        ISecurityPolicy? policy = null,
        X509Certificate2? clientCert = null,
        X509Certificate2? serverCert = null,
        MessageSecurityMode mode = MessageSecurityMode.None) : IDisposable, IAsyncDisposable
    {
        // Configuration
        private readonly string _endpointUrl = endpointUrl;
        private readonly IUserIdentity _userIdentity = userIdentity ?? new AnonymousIdentity("Anonymous");
        private readonly ISecurityPolicy _policy = policy ?? new SecurityPolicyNone();
        private readonly X509Certificate2? _clientCert = clientCert;
        private readonly X509Certificate2? _serverCert = serverCert;
        private readonly MessageSecurityMode _securityMode = mode;

        // Runtime State
        private UaTcpClientChannel? _channel;

        // Mapping: PublishingInterval -> Bucket
        private readonly ConcurrentDictionary<double, SubscriptionBucket> _buckets = new();

        // Handles
        private uint _nextClientHandle = 1;
        private readonly Lock _handleLock = new();

        // Lifecycle
        private CancellationTokenSource? _lifecycleCts;
        private Task? _reconnectTask;
        private volatile bool _isConnected = false;
        private readonly Lock _reconnectLock = new();
        private bool _isReconnecting = false;

        // Events
        public event Action<uint, DataValue>? DataChanged;
        public event Action<bool>? ConnectionStatusChanged;

        public void Start()
        {
            if (_lifecycleCts != null) return;
            _lifecycleCts = new CancellationTokenSource();
            _reconnectTask = Task.Run(SupervisorLoop);
        }

        public async Task<uint> SubscribeAsync(NodeId nodeId, double publishingInterval = 1000.0)
        {
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
                catch (Exception ex)
                {
                    throw;
                }
            }
            return handle;
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
                        ConnectionStatusChanged?.Invoke(true);
                    }
                    catch (Exception ex)
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

        private async Task ConnectAndRestoreAsync(CancellationToken cancellationToken)
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
            _channel = new UaTcpClientChannel(_endpointUrl, _policy, _clientCert, _serverCert, _securityMode);
            await _channel.ConnectAsync(cancellationToken);
            await _channel.CreateSessionAsync("urn:s7nexus:resilient", "urn:s7nexus", "Monitor", cancellationToken);
            await _channel.ActivateSessionAsync(_userIdentity, cancellationToken);

            // 3. Restore
            foreach (var bucket in _buckets.Values)
            {
                await bucket.EnsureSubscriptionCreatedAsync(_channel, OnDataChanged, OnSubscriptionError);
                await bucket.RestoreItemsAsync();
            }
        }

        // --- DISPOSE IMPLEMENTIERUNG ---

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
                    foreach (var kvp in _items)
                    {
                        // TODO: Bulk Create
                        LiveSubscription.CreateMonitoredItemAsync(kvp.Value, kvp.Key).Wait();
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