using LiteUa.Encoding;

namespace LiteUa.Transport.Headers
{
    /// <summary>
    /// Represents the asymmetric algorithm security header used in OPC UA secure communications.
    /// </summary>
    public class AsymmetricAlgorithmSecurityHeader
    {
        /// <summary>
        /// Gets or sets the URI of the security policy used for the asymmetric algorithm.
        /// </summary>
        public string? SecurityPolicyUri { get; set; }

        /// <summary>
        /// Gets or sets the sender's certificate in byte array format.
        /// </summary>
        public byte[]? SenderCertificate { get; set; } // Client Certificate

        /// <summary>
        /// Gets or sets the thumbprint of the receiver's certificate in byte array format.
        /// </summary>
        public byte[]? ReceiverCertificateThumbprint { get; set; } // Server Thumbprint

        /// <summary>
        /// Encodes the asymmetric algorithm security header using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteString(SecurityPolicyUri);
            writer.WriteByteString(SenderCertificate);
            writer.WriteByteString(ReceiverCertificateThumbprint);
        }
    }
}