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
    public class DiscoveryClient
    {
        private readonly string _endpointUrl;
        private readonly string _applicationUri;
        private readonly string _productUri;
        private readonly string _applicationName;
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
        public DiscoveryClient(string endpointUrl, string applicationUri, string productUri, string applicationName, IUaTcpClientChannelFactory clientChannelFactory)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpointUrl);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(productUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationName);

            _userIdentity = new AnonymousIdentityToken();
            _policyFactory = new SecurityPolicyFactoryNone();
            _securityMode = MessageSecurityMode.None;

            _endpointUrl = endpointUrl;
            _applicationUri = applicationUri;
            _productUri = productUri;
            _applicationName = applicationName;
            _clientChannelFactory = clientChannelFactory;
        }

        /// <summary>
        /// Gets the endpoint description that matches the specified security mode, policy URI, and user token type.
        /// </summary>
        /// <param name="targetSecurityMode">The target security mode.</param>
        /// <param name="targetPolicyUri">The target policy uri.</param>
        /// <param name="targetTokenType">The target UserTokenType.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the async operations.</param>
        /// <returns>An <see cref="EndpointDescription"/> that matches the specified requirements, otherwise null if none was found.</returns>
        public async Task<EndpointDescription?> GetEndpoint(MessageSecurityMode targetSecurityMode, string targetPolicyUri, UserTokenType targetTokenType, CancellationToken cancellationToken = default)
        {
            await using var discovery = _clientChannelFactory.CreateTcpClientChannel(_endpointUrl, _applicationUri, _productUri, _applicationName, _policyFactory, _securityMode, null, null);
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