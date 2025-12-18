using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Client.Pooling
{
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

        // Idle clients
        private readonly ConcurrentBag<UaTcpClientChannel> _clients;

        private readonly SemaphoreSlim _semaphore;

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
            int maxSize)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpointUrl);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(productUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationName);
            ArgumentNullException.ThrowIfNull(userIdentity);
            ArgumentNullException.ThrowIfNull(securityPolicyFactory);

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
            _clients = [];
            _semaphore = new(maxSize, maxSize);
        }

        public async Task<PooledUaClient> RentAsync()
        {
            // Wait for idle client
            await _semaphore.WaitAsync();

            // Try to get an existing client
            if (_clients.TryTake(out UaTcpClientChannel? client))
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

        internal void Return(PooledUaClient pooledClient)
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

        private async Task<UaTcpClientChannel> CreateNewClientAsync()
        {
            /// TODO: general connection logic via configurable Security Policies, Message Security Modes, etc.
            var client = new UaTcpClientChannel(_endpointUrl, _applicationUri, _productUri, _applicationName, _securityPolicyFactory, _securityMode, _clientCert, _serverCert);

            try
            {
                await client.ConnectAsync();
                await client.CreateSessionAsync("PoolSession");
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