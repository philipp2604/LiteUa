using LiteUa.BuiltIn;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Stack.Session.Identity
{
    public interface IUserIdentity
    {
        /// <summary>
        /// Transforms the user identity to an <see cref="ExtensionObject"/> for transmission.
        /// </summary>
        /// <param name="serverCertificate">The server certificate.</param>
        /// <param name="serverNonce">Ther server nonce.</param>
        /// <returns>The transformed <see cref="ExtensionObject"/></returns>
        ExtensionObject ToExtensionObject(X509Certificate2? serverCertificate, byte[]? serverNonce);
    }
}