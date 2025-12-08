using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.SecureChannel
{
    /// TODO: fix documentation comments

    public enum MessageSecurityMode : int
    {
        Invalid = 0,
        None = 1,
        Sign = 2,
        SignAndEncrypt = 3
    }
}
