using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Transport
{
    public interface IUaTcpClientChannelFactory
    {
        public IUaTcpClientChannel CreateTcpClientChannel(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            ISecurityPolicyFactory policyFactory,
            MessageSecurityMode securityMode,
            X509Certificate2? clientCertificate,
            X509Certificate2? serverCertificate);
    }
}
