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
    public class UaInnerClientsFactory : IUaInnerClientsFactory
    {
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
            uint minPublishTimeoutMs,
            IUaTcpClientChannelFactory clientChannelFactory,
            int supervisorIntervalMs = 1000,
            int reconnectIntervalMs = 5000
        ) => new SubscriptionClient(
                endpointUrl,
                applicationUri,
                productUri,
                applicationName,
                userIdentity,
                policyFactory,
                securityMode,
                clientCertificate,
                serverCertificate,
                heartbeatIntervalMs,
                heartbeatTimeoutHintMs,
                maxPublishRequests,
                publishTimeoutMultiplier,
                minPublishTimeoutMs,
                clientChannelFactory,
                supervisorIntervalMs,
                reconnectIntervalMs);

        public IDiscoveryClient CreateDiscoveryClient(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            uint heartbeatIntervalMs,
            uint heartbeatTimeoutHintMs,
            IUaTcpClientChannelFactory clientChannelFactory
        ) => new DiscoveryClient(
                endpointUrl,
                applicationUri,
                productUri,
                applicationName,
                heartbeatIntervalMs,
                heartbeatTimeoutHintMs,
                clientChannelFactory);

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
            IUaTcpClientChannelFactory tcpClientChannelFactory
        ) => new UaClientPool(
                endpointUrl,
                applicationUri,
                productUri,
                applicationName,
                userIdentity,
                securityPolicyFactory,
                securityMode,
                clientCert,
                serverCert,
                maxSize,
                heartbeatIntervalMs,
                heartbeatTimeoutHintMs,
                tcpClientChannelFactory);
    }
}