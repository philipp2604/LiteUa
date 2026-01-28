using LiteUa.Stack.Discovery;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;

namespace LiteUa.Client.Discovery
{
    public interface IDiscoveryClient
    {
        /// <summary>
        /// Gets the endpoint description that matches the specified security mode, policy URI, and user token type.
        /// </summary>
        /// <param name="targetSecurityMode">The target security mode.</param>
        /// <param name="targetPolicyUri">The target policy uri.</param>
        /// <param name="targetTokenType">The target UserTokenType.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the async operations.</param>
        /// <returns>An <see cref="EndpointDescription"/> that matches the specified requirements, otherwise null if none was found.</returns>
        public Task<EndpointDescription?> GetEndpoint(MessageSecurityMode targetSecurityMode, string targetPolicyUri, UserTokenType targetTokenType, CancellationToken cancellationToken = default);
    }
}