using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using Moq;

namespace LiteUa.Tests.UnitTests.Transport.Headers
{
    [Trait("Category", "Unit")]
    public class AsymmetricAlgorithmSecurityHeaderTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public AsymmetricAlgorithmSecurityHeaderTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Encode_ValidData_WritesCorrectSequence()
        {
            // Arrange
            var header = new AsymmetricAlgorithmSecurityHeader
            {
                SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256",
                SenderCertificate = [0x01, 0x02, 0x03],
                ReceiverCertificateThumbprint = [0x0A, 0x0B]
            };

            // Act
            header.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteString("http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256"), Times.Once);
            _writerMock.Verify(w => w.WriteByteString(It.Is<byte[]>(b => b.Length == 3)), Times.Once);
            _writerMock.Verify(w => w.WriteByteString(It.Is<byte[]>(b => b.Length == 2)), Times.Once);
        }

        [Fact]
        public void Encode_NullValues_CallsWriterWithNull()
        {
            // Arrange
            var header = new AsymmetricAlgorithmSecurityHeader
            {
                SecurityPolicyUri = null,
                SenderCertificate = null,
                ReceiverCertificateThumbprint = null
            };

            // Act
            header.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteString(null), Times.Once);
            _writerMock.Verify(w => w.WriteByteString(null), Times.Exactly(2));
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var header = new AsymmetricAlgorithmSecurityHeader
            {
                SecurityPolicyUri = "uri-marker",
                SenderCertificate = [0xAA],
                ReceiverCertificateThumbprint = [0xBB]
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteString("uri-marker"))
                       .Callback(() => callOrder.Add("URI"));

            _writerMock.Setup(w => w.WriteByteString(It.Is<byte[]>(b => b[0] == 0xAA)))
                       .Callback(() => callOrder.Add("SenderCert"));

            _writerMock.Setup(w => w.WriteByteString(It.Is<byte[]>(b => b[0] == 0xBB)))
                       .Callback(() => callOrder.Add("Thumbprint"));

            // Act
            header.Encode(_writerMock.Object);

            // Assert
            Assert.Equal("URI", callOrder[0]);
            Assert.Equal("SenderCert", callOrder[1]);
            Assert.Equal("Thumbprint", callOrder[2]);
        }
    }
}