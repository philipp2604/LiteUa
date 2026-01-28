using LiteUa.Security.Policies;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;

namespace LiteUa.Client.Discovery
{
    /// <summary>
    /// Provides functionality to discover OPC UA server endpoints and retrieve endpoint descriptions that match
    /// the specified security and user token requirements.
    /// </summary>
    public class DiscoveryClient : IDiscoveryClient
    {
        private readonly string _endpointUrl;
        private readonly string _applicationUri;
        private readonly string _productUri;
        private readonly string _applicationName;
        private readonly uint _heartbeatIntervalMs;
        private readonly uint _heartbeatTimeoutHintMs;
        private readonly IUserIdentity _userIdentity;
        private readonly ISecurityPolicyFactory _policyFactory;
        private readonly MessageSecurityMode _securityMode;
        private readonly IUaTcpClientChannelFactory _clientChannelFactory;

        /// <summary>
        /// Creates a new instance of the <see cref="DiscoveryClient"> class.
        /// </summary>
        /// <param name="endpointUrl">The server's endpoint url including protocol and port, e.g. 'opc.tcp://192.178.0.1:4840/'.</param>
        /// <param name="applicationUri">The application uri.</param>
        /// <param name="productUri">The product uri.</param>
        /// <param name="applicationName">The application name.</param>
        /// <param name="clientChannelFactory">An instance of <see cref="IUaTcpClientChannel"/>.</param>
        public DiscoveryClient(string endpointUrl, string applicationUri, string productUri, string applicationName, uint heartbeatIntervalMs, uint heartbeatTimeoutHintMs, IUaTcpClientChannelFactory clientChannelFactory)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpointUrl);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(productUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationName);
            ArgumentNullException.ThrowIfNull(clientChannelFactory);

            _userIdentity = new AnonymousIdentityToken();
            _policyFactory = new SecurityPolicyFactoryNone();
            _securityMode = MessageSecurityMode.None;

            _endpointUrl = endpointUrl;
            _applicationUri = applicationUri;
            _productUri = productUri;
            _applicationName = applicationName;
            _heartbeatIntervalMs = heartbeatIntervalMs;
            _heartbeatTimeoutHintMs = heartbeatTimeoutHintMs;
            _clientChannelFactory = clientChannelFactory;
        }

        public async Task<EndpointDescription?> GetEndpoint(MessageSecurityMode targetSecurityMode, string targetPolicyUri, UserTokenType targetTokenType, CancellationToken cancellationToken = default)
        {
            await using var discovery = _clientChannelFactory.CreateTcpClientChannel(_endpointUrl, _applicationUri, _productUri, _applicationName, _policyFactory, _securityMode, null, null, _heartbeatIntervalMs, _heartbeatTimeoutHintMs);
            await discovery.ConnectAsync(cancellationToken);
            var endpoints = await discovery.GetEndpointsAsync(cancellationToken);

            var filteredEndpoint = endpoints.Endpoints?.FirstOrDefault(e =>
            e.SecurityMode == targetSecurityMode
            && e.SecurityPolicyUri == targetPolicyUri
            && (e.UserIdentityTokens?.Any(t => t.TokenType == (int)targetTokenType) ?? false));

            return filteredEndpoint;
        }
    }
}