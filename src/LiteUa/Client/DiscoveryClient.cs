using LiteUa.Security;
using LiteUa.Security.Policies;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Client
{
    public class DiscoveryClient
    {

        // Configuration
        private readonly string _endpointUrl;
        private readonly string _applicationUri;
        private readonly string _productUri;
        private readonly string _applicationName;
        private readonly IUserIdentity _userIdentity;
        private readonly ISecurityPolicy _policy;
        private readonly MessageSecurityMode _securityMode;

        public DiscoveryClient(string endpointUrl, string applicationUri, string productUri, string applicationName)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpointUrl);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(productUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationName);

            _userIdentity = new AnonymousIdentity();
            _policy = new SecurityPolicyNone();
            _securityMode = MessageSecurityMode.None;

            _endpointUrl = endpointUrl;
            _applicationUri = applicationUri;
            _productUri = productUri;
            _applicationName = applicationName;
        }

        public async Task<EndpointDescription?> GetEndpoint(MessageSecurityMode targetSecurityMode, string targetPolicyUri, UserTokenType targetTokenType)
        {
            await using var discovery = new UaTcpClientChannel(_endpointUrl, _applicationUri, _productUri, _applicationName, _policy, _securityMode, null, null);
            await discovery.ConnectAsync();
            var endpoints = await discovery.GetEndpointsAsync();

            var filteredEndpoint = endpoints.Endpoints?.FirstOrDefault(e =>
            e.SecurityMode == targetSecurityMode
            && e.SecurityPolicyUri == targetPolicyUri
            && (e.UserIdentityTokens?.Any(t => t.TokenType == (int)targetTokenType && t.SecurityPolicyUri == (targetPolicyUri == SecurityPolicyUris.None ? null : targetPolicyUri)) ?? false));

            return filteredEndpoint;
        }
    }
}
