namespace LiteUa.Client.Pooling
{
    public interface IUaClientPool : IDisposable, IAsyncDisposable
    {
        Task<PooledUaClient> RentAsync();
    }
}