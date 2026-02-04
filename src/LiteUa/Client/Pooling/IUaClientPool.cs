namespace LiteUa.Client.Pooling
{
    /// <summary>
    /// Interface for the UaClientPool.
    /// </summary>
    public interface IUaClientPool : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Clears the pool by disposing all idle clients.
        /// </summary>
        public void Clear();

        /// <summary>
        /// Rents an UaTcpClientChannel from the pool. If none are available, a new one is created up to the max pool size.
        /// </summary>
        /// <returns>An instance of <see cref="PooledUaClient"/>.</returns>
        public Task<PooledUaClient> RentAsync();

        /// <summary>
        /// Returns a UaTcpClientChannel back to the pool.
        /// </summary>
        /// <param name="pooledClient">The instance to return.</param>
        public void Return(PooledUaClient pooledClient);
    }
}