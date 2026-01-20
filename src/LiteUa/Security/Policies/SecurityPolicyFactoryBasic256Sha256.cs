using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Security.Policies
{
    /// <summary>
    /// Factory for creating instances of the Basic256Sha256 security policy.
    /// </summary>
    public class SecurityPolicyFactoryBasic256Sha256 : ISecurityPolicyFactory
    {
        public ISecurityPolicy CreateSecurityPolicy(X509Certificate2? localCert, X509Certificate2? remoteCert)
        {
            ArgumentNullException.ThrowIfNull(localCert);
            ArgumentNullException.ThrowIfNull(remoteCert);

            return new SecurityPolicyBasic256Sha256(localCert, remoteCert);
        }
    }
}