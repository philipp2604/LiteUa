using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Security.Policies
{
    public interface ISecurityPolicyFactory
    {
        ISecurityPolicy CreateSecurityPolicy(X509Certificate2? localCert, X509Certificate2? remoteCert);
    }
}