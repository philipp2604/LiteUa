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
    /// <summary>
    /// A factory for creating <see cref="IUaTcpClientChannel"/> instances.
    /// </summary>
    public class UaTcpClientChannelFactory : IUaTcpClientChannelFactory
    {
        /// <summary>
        /// Creates and initializes a new instance of a <see cref="UaTcpClientChannel"/>.
        /// </summary>
        /// <param name="endpointUrl">The network address of the OPC UA server endpoint (e.g., "opc.tcp://localhost:4840").</param>
        /// <param name="applicationUri">The globally unique identifier (URI) for the client application instance.</param>
        /// <param name="productUri">The globally unique identifier (URI) for the client product.</param>
        /// <param name="applicationName">A human-readable name for the client application.</param>
        /// <param name="policyFactory">The factory responsible for creating the security policy and cryptographic providers.</param>
        /// <param name="securityMode">The message security mode (None, Sign, or SignAndEncrypt) to apply to the channel.</param>
        /// <param name="clientCertificate">The X.509 certificate of the client, used for signing and decryption.</param>
        /// <param name="serverCertificate">The X.509 certificate of the server, used for encryption and signature verification.</param>
        /// <returns>A new <see cref="IUaTcpClientChannel"/> instance configured with the specified parameters.</returns>
        public IUaTcpClientChannel CreateTcpClientChannel(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            ISecurityPolicyFactory policyFactory,
            MessageSecurityMode securityMode,
            X509Certificate2? clientCertificate,
            X509Certificate2? serverCertificate)
        {
            return new UaTcpClientChannel(
                endpointUrl,
                applicationUri,
                productUri,
                applicationName,
                policyFactory,
                securityMode,
                clientCertificate,
                serverCertificate);
        }
    }
}
