using LiteUa.BuiltIn;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Stack.Session.Identity
{
    /// TODO: fix documentation comments

    public interface IUserIdentity
    {
        ExtensionObject ToExtensionObject(X509Certificate2? serverCertificate, byte[]? serverNonce);
    }
}