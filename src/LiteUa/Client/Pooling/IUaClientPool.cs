using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Client.Pooling
{
    public interface IUaClientPool : IDisposable, IAsyncDisposable
    {
        Task<PooledUaClient> RentAsync();
    }
}
