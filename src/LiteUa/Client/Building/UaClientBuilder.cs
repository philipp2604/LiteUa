using LiteUa.Transport;

namespace LiteUa.Client.Building
{
    /// <summary>
    /// Provides a builder for configuring and creating instances of the UaClient class.
    /// </summary>
    public class UaClientBuilder
    {
        private readonly UaClientOptions _options = new();

        /// <summary>
        /// Specifies the endpoint URL for the UaClient.
        /// </summary>
        /// <param name="url">The endpoint url with protocol and port, like: 'opc.tcp://192.178.0.1:4840/'.</param>
        /// <returns></returns>
        public UaClientBuilder ForEndpoint(string url)
        {
            _options.EndpointUrl = url;
            return this;
        }

        /// <summary>
        /// Specifies security options for the UaClient.
        /// </summary>
        /// <param name="configure">The security options.</param>
        /// <returns></returns>
        public UaClientBuilder WithSecurity(Action<UaClientOptions.SecurityOptions> configure)
        {
            configure(_options.Security);
            return this;
        }

        /// <summary>
        /// Specifies session options for the UaClient.
        /// </summary>
        /// <param name="configure">The session options.</param>
        /// <returns></returns>
        public UaClientBuilder WithSession(Action<UaClientOptions.SessionOptions> configure)
        {
            configure(_options.Session);
            return this;
        }

        /// <summary>
        /// Specifies client pool options for the UaClient.
        /// </summary>
        /// <param name="configure">The client pool options.</param>
        /// <returns></returns>
        public UaClientBuilder WithPool(Action<UaClientOptions.PoolOptions> configure)
        {
            configure(_options.Pool);
            return this;
        }

        /// <summary>
        /// Creates and returns a new instance of the UaClient class using the configured options.
        /// </summary>
        /// <returns>A UaClient instance initialized with the current configuration options.</returns>
        public UaClient Build()
        {
            if (string.IsNullOrEmpty(_options.EndpointUrl))
                throw new InvalidOperationException("Endpoint URL must be set.");

            return new UaClient(_options, new UaTcpClientChannelFactory(), new UaInnerClientsFactory());
        }
    }
}