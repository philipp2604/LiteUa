using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Security.Policies
{
    public interface ISecurityPolicyFactory
    {
        /// <summary>
        /// Creates a security policy based on the provided local and remote certificates.
        /// </summary>
        /// <param name="localCert">The local <see cref="X509Certificate2"/> to use.</param>
        /// <param name="remoteCert">The remote <see cref="X509Certificate"/> to use.</param>
        /// <returns>A security policy instance implementing <see cref="ISecurityPolicy"/>.</returns>
        ISecurityPolicy CreateSecurityPolicy(X509Certificate2? localCert, X509Certificate2? remoteCert);
    }
}