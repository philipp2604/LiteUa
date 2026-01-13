namespace LiteUa.Client.Pooling
{
    /// <summary>
    /// Interface for the UaClientPool.
    /// </summary>
    public interface IUaClientPool : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Rents a UaClient from the pool.
        /// </summary>
        /// <returns>An UaClient instance from the pool.</returns>
        public Task<PooledUaClient> RentAsync();
    }
}