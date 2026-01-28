using LiteUa.Encoding;
using LiteUa.Stack.SecureChannel;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.SecureChannel
{
    [Trait("Category", "Unit")]
    public class OpenSecureChannelRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public OpenSecureChannelRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(446u, OpenSecureChannelRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var request = new OpenSecureChannelRequest();

            // Assert
            Assert.Equal(0u, request.ClientProtocolVersion);
            Assert.Equal(SecurityTokenRequestType.Issue, request.RequestType);
            Assert.Equal(MessageSecurityMode.None, request.SecurityMode);
            Assert.Equal(3600000u, request.RequestedLifetime);
            Assert.Null(request.ClientNonce);
        }

        [Fact]
        public void Encode_WritesCorrectValuesInSequence()
        {
            // Arrange
            var nonce = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            var request = new OpenSecureChannelRequest
            {
                ClientProtocolVersion = 1,
                RequestType = SecurityTokenRequestType.Renew,
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                ClientNonce = nonce,
                RequestedLifetime = 600000
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. Service ID
            _writerMock.Verify(w => w.WriteUInt16(446), Times.Once);

            // 2. Payload Fields
            _writerMock.Verify(w => w.WriteUInt32(1u), Times.Once);
            _writerMock.Verify(w => w.WriteInt32((int)SecurityTokenRequestType.Renew), Times.Once);
            _writerMock.Verify(w => w.WriteInt32((int)MessageSecurityMode.SignAndEncrypt), Times.Once);

            // 3. ClientNonce (ByteString)
            _writerMock.Verify(w => w.WriteByteString(nonce), Times.Once);

            // 4. RequestedLifetime
            _writerMock.Verify(w => w.WriteUInt32(600000u), Times.Once);
        }

        [Fact]
        public void Encode_NullNonce_WritesNullByteString()
        {
            // Arrange
            var request = new OpenSecureChannelRequest { ClientNonce = null };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // WriteByteString(null) internally writes -1 as Int32.
            _writerMock.Verify(w => w.WriteByteString(null), Times.Once);
        }

        [Fact]
        public void Encode_SequenceCheck_VerifiesFieldOrder()
        {
            // Arrange
            var request = new OpenSecureChannelRequest
            {
                ClientProtocolVersion = 99,
                RequestedLifetime = 8888
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteUInt32(99))
                       .Callback(() => callOrder.Add("Version"));

            _writerMock.Setup(w => w.WriteByteString(It.IsAny<byte[]>()))
                       .Callback(() => callOrder.Add("Nonce"));

            _writerMock.Setup(w => w.WriteUInt32(8888))
                       .Callback(() => callOrder.Add("Lifetime"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int versionIdx = callOrder.IndexOf("Version");
            int nonceIdx = callOrder.IndexOf("Nonce");
            int lifetimeIdx = callOrder.IndexOf("Lifetime");

            Assert.True(versionIdx < nonceIdx);
            Assert.True(nonceIdx < lifetimeIdx);
        }
    }
}