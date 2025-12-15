using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Client
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
            public string? Username = null;
            public string? Password = null;
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

    public class UaClientBuilder
    {
        private readonly UaClientOptions _options = new();

        public UaClientBuilder ForEndpoint(string url)
        {
            _options.EndpointUrl = url;
            return this;
        }

        public UaClientBuilder WithSecurity(Action<UaClientOptions.SecurityOptions> configure)
        {
            configure(_options.Security);
            return this;
        }

        public UaClientBuilder WithSession(Action<UaClientOptions.SessionOptions> configure)
        {
            configure(_options.Session);
            return this;
        }

        public UaClientBuilder WithPool(Action<UaClientOptions.PoolOptions> configure)
        {
            configure(_options.Pool);
            return this;
        }

        public UaClient Build()
        {
            if (string.IsNullOrEmpty(_options.EndpointUrl))
                throw new InvalidOperationException("Endpoint URL must be set.");

            return new UaClient(_options);
        }
    }
}
