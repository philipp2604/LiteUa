using LiteUa.BuiltIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Session.Identity
{
    /// TODO: fix documentation comments

    public interface IUserIdentity
    {
        ExtensionObject ToExtensionObject(X509Certificate2? serverCertificate, byte[]? serverNonce);
    }
}
