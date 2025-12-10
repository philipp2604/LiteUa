using LiteUa.BuiltIn;
using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Client
{
    public class SubscriptionClient : IDisposable, IAsyncDisposable
    {
        private readonly string _endpointUrl;
        private readonly IUserIdentity _userIdentity;
        private readonly X509Certificate2? _clientCert;
        private readonly X509Certificate2? _serverCert;
        private readonly ISecurityPolicy _policy;
        private readonly MessageSecurityMode _securityMode;

        private UaTcpClientChannel? _channel;
        private Subscription? _subscription;

        // State Management
        private readonly Dictionary<uint, NodeId> _monitoredItems = new Dictionary<uint, NodeId>();
        private readonly object _itemsLock = new object();
        private uint _nextClientHandle = 1;

        private CancellationTokenSource? _lifecycleCts;
        private Task? _reconnectTask;
        private volatile bool _isConnected = false;

        // Events
        public event Action<uint, DataValue>? DataChanged;
        public event Action<bool>? ConnectionStatusChanged; // True = Connected, False = Reconnecting

        public SubscriptionClient(
            string endpointUrl,
            IUserIdentity? userIdentity = null,
            ISecurityPolicy? policy = null,
            X509Certificate2? clientCert = null,
            X509Certificate2? serverCert = null,
            MessageSecurityMode mode = MessageSecurityMode.None)
        {
            _endpointUrl = endpointUrl;
            _userIdentity = userIdentity ?? new AnonymousIdentity("Anonymous");
            _policy = policy ?? new SecurityPolicyNone();
            _clientCert = clientCert;
            _serverCert = serverCert;
            _securityMode = mode;
        }

        public void Start()
        {
            if (_lifecycleCts != null) return; // already started

            _lifecycleCts = new CancellationTokenSource();
            _reconnectTask = Task.Run(SupervisorLoop);
        }

        public async Task<uint> SubscribeAsync(NodeId nodeId)
        {
            uint handle;
            lock (_itemsLock)
            {
                handle = _nextClientHandle++;
                _monitoredItems[handle] = nodeId;
            }
            if (_isConnected && _subscription != null)
            {
                try
                {
                    await _subscription.CreateMonitoredItemAsync(nodeId, handle);
                }
                catch
                {
                    throw;
                }
            }

            return handle;
        }

        private async Task SupervisorLoop()
        {
            if(_lifecycleCts == null) throw new InvalidOperationException("Client not started.");

            while (!_lifecycleCts.IsCancellationRequested)
            {
                if (!_isConnected)
                {
                    try
                    {
                        await ConnectAndSetupAsync();

                        _isConnected = true;
                        ConnectionStatusChanged?.Invoke(true);
                    }
                    catch (Exception)
                    {
                        // Backoff
                        await Task.Delay(5000, _lifecycleCts.Token);
                    }
                }
                else
                {
                    await Task.Delay(1000, _lifecycleCts.Token);
                }
            }
        }

        private async Task ConnectAndSetupAsync()
        {
            // 1. Cleanup Old
            _channel?.Dispose();
            _subscription = null;

            // 2. Connect Stack
            _channel = new UaTcpClientChannel(_endpointUrl, _policy, _clientCert, _serverCert, _securityMode);
            await _channel.ConnectAsync();
            await _channel.CreateSessionAsync("urn:s7nexus:resilient", "urn:s7nexus", "Monitor");
            await _channel.ActivateSessionAsync(_userIdentity);

            // 3. Setup Subscription
            var sub = new Subscription(_channel);
            sub.DataChanged += (h, v) => DataChanged?.Invoke(h, v);
            sub.ConnectionLost += OnConnectionLost;

            await sub.CreateAsync(1000);

            // 4. Restore Items
            lock (_itemsLock)
            {
                foreach (var kvp in _monitoredItems)
                {

                    /// TODO: Bulk CreateMonitoredItems

                    sub.CreateMonitoredItemAsync(kvp.Value, kvp.Key).Wait();
                }
            }

            _subscription = sub;
        }

        private void OnConnectionLost(Exception ex)
        {
            Console.WriteLine("Connection Lost detected!");
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(false);
        }

        public void Dispose()
        {
            _lifecycleCts?.Cancel();
            _channel?.DisconnectAsync().Wait();
            _channel?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if(_channel != null)
                await _channel.DisposeAsync();

            Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
