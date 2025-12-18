namespace LiteUa.Client.Building
{
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