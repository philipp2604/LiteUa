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
using System.Threading.Tasks;

namespace LiteUa.Client.Pooling
{
    public class UaClientPool : IUaClientPool
    {
        private readonly string _endpointUrl;
        private readonly string _applicationUri;
        private readonly string _productUri;
        private readonly string _applicationName;
        private readonly ISecurityPolicy _securityPolicy;
        private readonly IUserIdentity _userIdentity;
        private readonly MessageSecurityMode _messageSecurityMode;
        private readonly X509Certificate2? _clientCertificate;
        private readonly X509Certificate2? _serverCertificate;
        private readonly int _maxSize;

        // Idle clients
        private readonly ConcurrentBag<UaTcpClientChannel> _clients = [];
        private readonly SemaphoreSlim _semaphore;

        public UaClientPool(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            ISecurityPolicy securityPolicy,
            IUserIdentity userIdentity,
            MessageSecurityMode securityMode,
            X509Certificate2? clientCertificate,
            X509Certificate2? serverCertificate,
            int maxSize = 10)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpointUrl);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(productUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationName);
            ArgumentNullException.ThrowIfNull(securityPolicy);
            ArgumentNullException.ThrowIfNull(userIdentity);

            _endpointUrl = endpointUrl;
            _applicationUri = applicationUri;
            _productUri = productUri;
            _applicationName = applicationName;
            _securityPolicy = securityPolicy;
            _userIdentity = userIdentity;
            _messageSecurityMode = securityMode;
            _clientCertificate = clientCertificate;
            _serverCertificate = serverCertificate;
            _maxSize = maxSize;
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

            var client = new UaTcpClientChannel(_endpointUrl, _applicationUri, _productUri, _applicationName,
                _securityPolicy, _messageSecurityMode, _clientCertificate, _serverCertificate);

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
