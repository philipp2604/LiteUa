using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Security.Policies
{
    public class SecurityPolicyFactoryNone : ISecurityPolicyFactory
    {
        public ISecurityPolicy CreateSecurityPolicy(X509Certificate2? localCert, X509Certificate2? remoteCert)
        {
            return new SecurityPolicyNone();
        }
    }
}