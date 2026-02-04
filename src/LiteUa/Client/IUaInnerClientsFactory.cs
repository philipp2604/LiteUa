using LiteUa.Client.Discovery;
using LiteUa.Client.Pooling;
using LiteUa.Client.Subscriptions;
using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Client
{
    public interface IUaInnerClientsFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="ISubscriptionClient">.
        /// </summary>
        /// <param name="endpointUrl">The server's endpoint url including protocol and port, e.g. 'opc.tcp://192.178.0.1:4840/'.</param>
        /// <param name="applicationUri">The application uri.</param>
        /// <param name="productUri">The product uri.</param>
        /// <param name="applicationName">The application name.</param>
        /// <param name="userIdentity">The user identity of type <see cref="IUserIdentity"/> to use.</param>
        /// <param name="policyFactory">An instance of <see cref="ISecurityPolicy"/>.</param>
        /// <param name="securityMode">The message security mode to use.</param>
        /// <param name="clientCertificate">Optional: The client's certificate.</param>
        /// <param name="serverCertificate">Optional: The server's certificate.</param>
        /// <param name="heartbeatIntervalMs">The heartbeat interval in milliseconds.</param>
        /// <param name="heartbeatTimeoutHintMs">The heartbeat timeout hint in milliseconds.</param>
        /// <param name="maxPublishRequests">The maximum number of concurrent publish requests.</param>
        /// <param name="publishTimeoutMultiplier">The multiplier to calculate the publish timeout.</param>
        /// <param name="minPubsubTimeoutMs">The minimum publish timeout in milliseconds.</param>
        /// <param name="clientChannelFactory">An instance of <see cref="IUaTcpClientChannelFactory"/>.</param>
        /// <param name="supervisorIntervalMs">The interval in milliseconds for the supervisor loop to check connection status.</param>
        /// <param name="reconnectIntervalMs">The interval in milliseconds to wait before attempting to reconnect after a disconnection.</param>
        public ISubscriptionClient CreateSubscriptionClient(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            IUserIdentity userIdentity,
            ISecurityPolicyFactory policyFactory,
            MessageSecurityMode securityMode,
            X509Certificate2? clientCertificate,
            X509Certificate2? serverCertificate,
            uint heartbeatIntervalMs,
            uint heartbeatTimeoutHintMs,
            uint maxPublishRequests,
            double publishTimeoutMultiplier,
            uint minPubsubTimeoutMs,
            IUaTcpClientChannelFactory clientChannelFactory,
            uint supervisorIntervalMs,
            uint reconnectIntervalMs);

        /// <summary>
        /// Creates a new instance of <see cref="IDiscoveryClient">.
        /// </summary>
        /// <param name="endpointUrl">The server's endpoint url including protocol and port, e.g. 'opc.tcp://192.178.0.1:4840/'.</param>
        /// <param name="applicationUri">The application uri.</param>
        /// <param name="productUri">The product uri.</param>
        /// <param name="applicationName">The application name.</param>
        /// <param name="heartbeatIntervalMs">The heartbeat interval in milliseconds.</param>
        /// <param name="heartbeatTimeoutHintMs">The heartbeat timeout hint in milliseconds.</param>
        /// <param name="clientChannelFactory">An instance of <see cref="IUaTcpClientChannel"/>.</param>
        public IDiscoveryClient CreateDiscoveryClient(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            uint heartbeatIntervalMs,
            uint heartbeatTimeoutHintMs,
            IUaTcpClientChannelFactory clientChannelFactory
        );

        /// <summary>
        /// Creates a new instance of <see cref="IUaClientPool"/> class.
        /// </summary>
        /// <param name="endpointUrl">The server's endpoint url including protocol and port, e.g. 'opc.tcp://192.178.0.1:4840/'.</param>
        /// <param name="applicationUri">The application uri.</param>
        /// <param name="productUri">The product uri.</param>
        /// <param name="applicationName">The application name.</param>
        /// <param name="userIdentity">The user identity of type <see cref="IUserIdentity"/> to use.</param>
        /// <param name="securityPolicyFactory">An instance of <see cref="ISecurityPolicy"/>.</param>
        /// <param name="securityMode">The message security mode to use.</param>
        /// <param name="clientCert">Optional: The client's certificate.</param>
        /// <param name="serverCert">Optional: The server's certificate.</param>
        /// <param name="maxSize">The max. pool size.</param>
        /// <param name="heartbeatIntervalMs">The heartbeat interval in milliseconds.</param>
        /// <param name="heartbeatTimeoutHintMs">The heartbeat timeout hint in milliseconds.</param>
        /// <param name="tcpClientChannelFactory">An instance of <see cref="IUaTcpClientChannelFactory"/>.</param>
        public IUaClientPool CreateUaClientPool(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            IUserIdentity userIdentity,
            ISecurityPolicyFactory securityPolicyFactory,
            MessageSecurityMode securityMode,
            X509Certificate2? clientCert,
            X509Certificate2? serverCert,
            int maxSize,
            uint heartbeatIntervalMs,
            uint heartbeatTimeoutHintMs,
            IUaTcpClientChannelFactory tcpClientChannelFactory);
    }
}