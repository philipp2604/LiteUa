using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Client.Pooling
{
    /// <summary>
    /// A pool for managing UaTcpClientChannel instances to efficiently reuse connections to an OPC UA server.
    /// </summary>
    public class UaClientPool : IUaClientPool
    {
        private readonly string _endpointUrl;
        private readonly string _applicationUri;
        private readonly string _productUri;
        private readonly string _applicationName;
        private readonly IUserIdentity _userIdentity;
        private readonly ISecurityPolicyFactory _securityPolicyFactory;
        private readonly X509Certificate2? _clientCert;
        private readonly X509Certificate2? _serverCert;
        private readonly MessageSecurityMode _securityMode;
        private readonly int _maxSize;
        private readonly uint _heartbeatIntervalMs;
        private readonly uint _heartbeatTimeoutHintMs;
        private readonly IUaTcpClientChannelFactory _tcpClientChannelFactory;

        // Idle clients
        private readonly ConcurrentBag<IUaTcpClientChannel> _clients;

        private readonly SemaphoreSlim _semaphore;

        /// <summary>
        /// Creates a new instance of the <see cref="UaClientPool"/> class.
        /// </summary>
        /// <param name="endpointUrl">The server's endpoint url including protocol and port, e.g. 'opc.tcp://192.178.0.1:4840/'.</param>
        /// <param name="applicationUri">The application uri.</param>
        /// <param name="productUri">The product uri.</param>
        /// <param name="applicationName">The application name.</param>
        /// <param name="userIdentity">The user identity of type <see cref="IUserIdentity"/> to use.</param>
        /// <param name="securityPolicyFactory">An instance of <see cref="ISecurityPolicy"/>.</param>
        /// <param name="securityMode">The message security mode to use.</param>
        /// <param name="clientCert">Optional: The client's certificate.</param>
        /// <param name="serverCert">Optional: The server's certificate.</param>
        /// <param name="maxSize">The max. pool size.</param>
        /// <param name="heartbeatIntervalMs"">The heartbeat interval in milliseconds.</param>
        /// <param name="heartbeatTimeoutHintMs">The heartbeat timeout hint in milliseconds.</param>
        /// <param name="tcpClientChannelFactory">An instance of <see cref="IUaTcpClientChannelFactory"/>.</param>
        public UaClientPool(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            IUserIdentity userIdentity,
            ISecurityPolicyFactory securityPolicyFactory,
            MessageSecurityMode securityMode,
            X509Certificate2? clientCert,
            X509Certificate2? serverCert,
            int maxSize,
            uint heartbeatIntervalMs,
            uint heartbeatTimeoutHintMs,
            IUaTcpClientChannelFactory tcpClientChannelFactory)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpointUrl);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(productUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationName);
            ArgumentNullException.ThrowIfNull(userIdentity);
            ArgumentNullException.ThrowIfNull(securityPolicyFactory);
            ArgumentNullException.ThrowIfNull(tcpClientChannelFactory);

            _endpointUrl = endpointUrl;
            _applicationUri = applicationUri;
            _productUri = productUri;
            _applicationName = applicationName;
            _userIdentity = userIdentity;
            _securityPolicyFactory = securityPolicyFactory;
            _clientCert = clientCert;
            _serverCert = serverCert;
            _securityMode = securityMode;
            _maxSize = maxSize;
            _heartbeatIntervalMs = heartbeatIntervalMs;
            _heartbeatTimeoutHintMs = heartbeatTimeoutHintMs;
            _clients = [];
            _semaphore = new(maxSize, maxSize);
            _tcpClientChannelFactory = tcpClientChannelFactory;
        }

        public async Task<PooledUaClient> RentAsync()
        {
            // Wait for idle client
            await _semaphore.WaitAsync();

            // Try to get an existing client
            if (_clients.TryTake(out IUaTcpClientChannel? client))
            {
                /// TODO: Quick "IsConnected" check here, e.g. a read on ServerStatus node.
                /// If broken -> Dispose and create a new one.
                return new PooledUaClient(client, this);
            }

            // None available, create a new one. Semaphore takes care of max size.
            try
            {
                client = await CreateNewClientAsync();
                return new PooledUaClient(client, this);
            }
            catch
            {
                _semaphore.Release(); // Exception occured, free slot
                throw;
            }
        }

        public void Return(PooledUaClient pooledClient)
        {
            if (pooledClient.IsInvalid)
            {
                // Dispose broken client
                pooledClient.InnerClient.Dispose();
                _semaphore.Release(); // release slot
            }
            else
            {
                // return to pool
                _clients.Add(pooledClient.InnerClient);
                _semaphore.Release();
            }
        }

        private async Task<IUaTcpClientChannel> CreateNewClientAsync()
        {
            var client = _tcpClientChannelFactory.CreateTcpClientChannel(_endpointUrl, _applicationUri, _productUri, _applicationName, _securityPolicyFactory, _securityMode, _clientCert, _serverCert, _heartbeatIntervalMs, _heartbeatTimeoutHintMs);

            try
            {
                await client.ConnectAsync();
                await client.CreateSessionAsync(_applicationName + " - PoolSession");
                await client.ActivateSessionAsync(_userIdentity);
                return client;
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            while (_clients.TryTake(out var client))
            {
                client.Dispose();
            }
            _semaphore.Dispose();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            while (_clients.TryTake(out var client))
            {
                await client.DisposeAsync();
            }
            Dispose();
            GC.SuppressFinalize(this);
        }
    }
}