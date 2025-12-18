using LiteUa.Encoding;

namespace LiteUa.Transport.Headers
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class AsymmetricAlgorithmSecurityHeader
    {
        public string? SecurityPolicyUri { get; set; }
        public byte[]? SenderCertificate { get; set; } // Client Certificate
        public byte[]? ReceiverCertificateThumbprint { get; set; } // Server Thumbprint

        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteString(SecurityPolicyUri);
            writer.WriteByteString(SenderCertificate);
            writer.WriteByteString(ReceiverCertificateThumbprint);
        }
    }
}