using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Discovery
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public enum ApplicationType : int
    {
        Server = 0,
        Client = 1,
        ClientAndServer = 2,
        DiscoveryServer = 3
    }
}
