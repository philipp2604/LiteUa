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
    /// An interface for creating OPC UA TCP client channels.
    /// </summary>
    public interface IUaTcpClientChannelFactory
    {
        /// <summary>
        /// Creates a new OPC UA TCP client channel with the specified parameters.
        /// </summary>
        /// <param name="endpointUrl">The network address of the OPC UA server endpoint (e.g., "opc.tcp://localhost:4840").</param>
        /// <param name="applicationUri">The globally unique identifier (URI) for the client application instance.</param>
        /// <param name="productUri">The globally unique identifier (URI) for the client product.</param>
        /// <param name="applicationName">A human-readable name for the client application to be passed to the server.</param>
        /// <param name="policyFactory">The factory responsible for creating the security policy and cryptographic providers.</param>
        /// <param name="securityMode">The message security mode (None, Sign, or SignAndEncrypt) to apply to the communication.</param>
        /// <param name="clientCertificate">The X.509 certificate of the client, typically including the private key for signing and decryption.</param>
        /// <param name="serverCertificate">The X.509 certificate of the server used for encryption and signature verification.</param>
        /// <param name="heartbeatIntervalMs">The interval in milliseconds at which heartbeat messages are sent to maintain the connection.</param>
        /// <param name="heartbeatTimeoutHintMs">The timeout hint in milliseconds indicating how long to wait for a heartbeat response before considering the connection lost.</param>
        /// <returns>An instance of <see cref="IUaTcpClientChannel"/> configured with the specified security and connection parameters.</returns>
        public IUaTcpClientChannel CreateTcpClientChannel(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            ISecurityPolicyFactory policyFactory,
            MessageSecurityMode securityMode,
            X509Certificate2? clientCertificate,
            X509Certificate2? serverCertificate,
            uint heartbeatIntervalMs,
            uint heartbeatTimeoutHintMs);
    }
}
