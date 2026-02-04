using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Client.Building
{
    /// <summary>
    /// Provides configuration options for establishing and managing a connection to an OPC UA server, including
    /// endpoint, security, session, and connection pool settings.
    /// </summary>
    public class UaClientOptions : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The endpoint URL of the OPC UA server to connect to including protocol and port: 'opc.tcp://127.0.0.1:4840/'.
        /// </summary>
        public string EndpointUrl { get; set; } = string.Empty;

        /// <summary>
        /// The security options for the UaClient.
        /// </summary>
        public SecurityOptions Security { get; } = new();

        /// <summary>
        /// The session options for the UaClient.
        /// </summary>
        public SessionOptions Session { get; } = new();

        /// <summary>
        /// The client pool options for the UaClient.
        /// </summary>
        public PoolOptions Pool { get; } = new();

        /// <summary>
        /// The transport limits for the UaClient.
        /// </summary>
        public TransportLimits Limits { get; } = new();

        public async ValueTask DisposeAsync()
        {
            await Security.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Represents a set of options for configuring security settings for the UaClient.
        /// </summary>
        public class SecurityOptions : IDisposable, IAsyncDisposable
        {
            /// <summary>
            /// Gets or sets a value indicating whether connections should automatically accept untrusted SSL
            /// certificates.
            /// </summary>
            public bool AutoAcceptUntrustedCertificates { get; set; } = false;

            /// <summary>
            /// Gets or sets the message security mode to be used for the connection.
            /// </summary>
            public MessageSecurityMode MessageSecurityMode { get; set; } = MessageSecurityMode.None;

            /// <summary>
            /// Gets or sets the security policy type to be used for the connection.
            /// </summary>
            public SecurityPolicyType PolicyType { get; set; } = SecurityPolicyType.None;

            /// <summary>
            /// Gets or sets the client certificate to be used for authentication.
            /// </summary>
            public X509Certificate2? ClientCertificate { get; set; }

            /// <summary>
            /// Gets or sets the server certificate to be used for validating the server's identity.
            /// </summary>
            public X509Certificate2? ServerCertificate { get; set; } /// TODO: Optional: Pinning

            /// <summary>
            /// Gets or sets the user token options for the connection.
            /// </summary>
            public UserTokenType UserTokenType { get; set; } = UserTokenType.Anonymous;

            /// <summary>
            /// Gets or sets the security policy type for the user token.
            /// </summary>
            public SecurityPolicyType UserTokenPolicyType { get; set; } = SecurityPolicyType.None;

            /// <summary>
            /// Gets or sets the username for UserName identity token authentication.
            /// </summary>
            public string? Username { get; set; } = null;

            /// <summary>
            /// Gets or sets the password for UserName identity token authentication.
            /// </summary>
            public string? Password { get; set; } = null;

            public async ValueTask DisposeAsync()
            {
                if (ClientCertificate != null)
                {
                    await Task.Run(() => ClientCertificate.Dispose());
                    ClientCertificate = null;
                }
                if (ServerCertificate != null)
                {
                    await Task.Run(() => ServerCertificate.Dispose());
                    ServerCertificate = null;
                }
                GC.SuppressFinalize(this);
            }

            public void Dispose()
            {
                DisposeAsync().AsTask().Wait();
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Represents a set of options for configuring session settings for the UaClient.
        /// </summary>
        public class SessionOptions
        {
            /// <summary>
            /// Gets or sets the name of the application establishing the session.
            /// </summary>
            public string ApplicationName { get; set; } = "LiteUa Client";

            /// <summary>
            /// Gets or sets the application URI of the application establishing the session.
            /// </summary>
            public string ApplicationUri { get; set; } = "urn:LiteUa:client";

            /// <summary>
            /// Gets or sets the product URI of the application establishing the session.
            /// </summary>
            public string ProductUri { get; set; } = "urn:github.com/philipp2604/LiteUa";
        }

        /// <summary>
        /// Represents a set of options for configuring client pool settings for the UaClient.
        /// </summary>
        public class PoolOptions
        {
            /// <summary>
            /// Gets or sets the maximum number of UaClient instances to maintain in the pool.
            /// </summary>
            public int MaxSize { get; set; } = 10;
        }

        /// <summary>
        /// Represents a set of options for configuring transport limits for the UaClient.
        /// </summary>
        public class TransportLimits
        {
            /// <summary>
            /// Gets or sets the heartbeat interval in milliseconds for maintaining the connection.
            /// </summary>
            public uint HeartbeatIntervalMs { get; set; } = 20000;

            /// <summary>
            /// Gets or sets the heartbeat timeout hint in milliseconds for detecting lost connections.
            /// </summary>
            public uint HeartbeatTimeoutHintMs { get; set; } = 10000;

            /// <summary>
            /// Gets or sets the maximum number of concurrent publish requests that can be outstanding at any given time.
            /// </summary>
            public uint MaxPublishRequestCount { get; set; } = 3;

            /// <summary>
            /// Gets or sets the multiplier used to calculate the publish timeout based on the publishing interval and the keepalive count.
            /// </summary>
            public double PublishTimeoutMultiplier { get; set; } = 2.0;

            /// <summary>
            /// Gets or sets the minimum publish timeout in milliseconds to ensure timely processing of publish requests.
            /// </summary>
            public uint MinPublishTimeoutMs { get; set; } = 10000;

            /// <summary>
            /// Gets or sets the interval in milliseconds for the supervisor loop to check connection status.
            /// </summary>
            public uint SupervisorIntervalMs { get; set; } = 5000;

            /// <summary>
            /// Gets or sets the interval in milliseconds to wait before attempting to reconnect after a disconnection.
            /// </summary>
            public uint ReconnectIntervalMs { get; set; } = 1000;
        }
    }
}