using LiteUa.Security.Policies;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;

namespace LiteUa.Client.Discovery
{
    public class DiscoveryClient : IAsyncDisposable, IDisposable
    {
        private readonly string _endpointUrl;
        private readonly string _applicationUri;
        private readonly string _productUri;
        private readonly string _applicationName;
        private readonly IUserIdentity _userIdentity;
        private readonly ISecurityPolicyFactory _policyFactory;
        private readonly MessageSecurityMode _securityMode;

        public DiscoveryClient(string endpointUrl, string applicationUri, string productUri, string applicationName)
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
        }

        public async Task<EndpointDescription?> GetEndpoint(MessageSecurityMode targetSecurityMode, string targetPolicyUri, UserTokenType targetTokenType, CancellationToken cancellationToken = default)
        {
            await using var discovery = new UaTcpClientChannel(_endpointUrl, _applicationUri, _productUri, _applicationName, _policyFactory, _securityMode, null, null);
            await discovery.ConnectAsync(cancellationToken);
            var endpoints = await discovery.GetEndpointsAsync(cancellationToken);

            var filteredEndpoint = endpoints.Endpoints?.FirstOrDefault(e =>
            e.SecurityMode == targetSecurityMode
            && e.SecurityPolicyUri == targetPolicyUri
            && (e.UserIdentityTokens?.Any(t => t.TokenType == (int)targetTokenType) ?? false));

            return filteredEndpoint;
        }

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
            GC.SuppressFinalize(this);
        }
    }
}