using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Client.Pooling
{
    public class UaClientPool(string endpointUrl, int maxSize = 10, IUserIdentity? userIdentity = null) : IUaClientPool
    {
        private readonly string _endpointUrl = endpointUrl;
        private readonly int _maxSize = maxSize;
        private readonly IUserIdentity _userIdentity = userIdentity ?? new AnonymousIdentity("Anonymous");

        // Idle clients
        private readonly ConcurrentBag<UaTcpClientChannel> _clients = [];
        private readonly SemaphoreSlim _semaphore = new(maxSize, maxSize);

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

            var client = new UaTcpClientChannel(_endpointUrl); /// TODO: DI

            try
            {
                await client.ConnectAsync();
                await client.CreateSessionAsync("urn:pool", "urn:pool", "PoolSession");
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
