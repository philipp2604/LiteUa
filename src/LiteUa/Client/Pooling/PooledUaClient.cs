using LiteUa.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Client.Pooling
{
    public class PooledUaClient(UaTcpClientChannel client, UaClientPool pool) : IDisposable, IAsyncDisposable
    {
        private readonly UaClientPool _pool = pool;
        public UaTcpClientChannel InnerClient { get; } = client;

        public bool IsInvalid { get; set; }

        public void Dispose()
        {
            _pool.Return(this);
            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }
}
