using LiteUa.Transport;

namespace LiteUa.Client.Pooling
{

    /// <summary>
    /// Represents a pooled OPC UA TCP client channel.
    /// </summary>
    /// <param name="client">The underlying transport channel.</param>
    /// <param name="pool">The client pool.</param>
    public class PooledUaClient(IUaTcpClientChannel client, IUaClientPool pool) : IDisposable, IAsyncDisposable
    {
        private readonly IUaClientPool _pool = pool;
        public IUaTcpClientChannel InnerClient { get; } = client;

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