using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Client.Building
{
    public class UaClientOptions
    {
        public string EndpointUrl { get; set; } = string.Empty;
        public SecurityOptions Security { get; } = new();
        public SessionOptions Session { get; } = new();
        public PoolOptions Pool { get; } = new();

        public class SecurityOptions
        {
            public bool AutoAcceptUntrustedCertificates { get; set; } = false;
            public MessageSecurityMode MessageSecurityMode { get; set; } = MessageSecurityMode.None;
            public SecurityPolicyType PolicyType { get; set; } = SecurityPolicyType.None;
            public X509Certificate2? ClientCertificate { get; set; }
            public X509Certificate2? ServerCertificate { get; set; } // Optional: Pinning
            public UserTokenType UserTokenType { get; set; } = UserTokenType.Anonymous;
            public SecurityPolicyType UserTokenPolicyType { get; set; } = SecurityPolicyType.None;
            public string? Username { get; set; } = null;
            public string? Password { get; set; } = null;
        }

        public class SessionOptions
        {
            public string ApplicationName { get; set; } = "LiteUa Client";
            public string ApplicationUri { get; set; } = "urn:LiteUa:client";
            public string ProductUri { get; set; } = "urn:github.com/philipp2604/LiteUa";
        }

        public class PoolOptions
        {
            public int MaxSize { get; set; } = 10;
        }
    }
}