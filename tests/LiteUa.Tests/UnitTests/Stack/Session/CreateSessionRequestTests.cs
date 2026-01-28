using LiteUa.Encoding;
using LiteUa.Stack.Session;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Session
{
    [Trait("Category", "Unit")]
    public class CreateSessionRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public CreateSessionRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(461u, CreateSessionRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var request = new CreateSessionRequest();

            // Assert
            Assert.Equal(60000.0, request.RequestedSessionTimeout);
            Assert.Equal(0u, request.MaxResponseMessageSize);
            Assert.NotNull(request.RequestHeader);
        }

        [Fact]
        public void Encode_MissingClientDescription_ThrowsArgumentNullException()
        {
            // Arrange
            var request = new CreateSessionRequest { ClientDescription = null };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => request.Encode(_writerMock.Object));
        }

        [Fact]
        public void Encode_FullRequest_WritesCorrectSequence()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                ClientDescription = new ClientDescription { ApplicationUri = "urn:client" },
                ServerUri = "urn:server",
                EndpointUrl = "opc.tcp://localhost",
                SessionName = "TestSession",
                ClientNonce = [0x1, 0x2],
                ClientCertificate = [0xA, 0xB],
                RequestedSessionTimeout = 5000.5,
                MaxResponseMessageSize = 1024
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. Service ID
            _writerMock.Verify(w => w.WriteUInt16(461), Times.Once);

            // 2. Specific Strings (sentinel values)
            _writerMock.Verify(w => w.WriteString("urn:server"), Times.Once);
            _writerMock.Verify(w => w.WriteString("opc.tcp://localhost"), Times.Once);
            _writerMock.Verify(w => w.WriteString("TestSession"), Times.Once);

            // 3. ByteStrings
            _writerMock.Verify(w => w.WriteByteString(It.Is<byte[]>(b => b.Length == 2)), Times.Exactly(2));

            // 4. Double / UInt32
            _writerMock.Verify(w => w.WriteDouble(5000.5), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(1024u), Times.Once);
        }

        [Fact]
        public void Encode_NullOptionals_WritesCorrectNullMarkers()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                ClientDescription = new ClientDescription(),
                ServerUri = null,
                ClientNonce = null
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteString(null), Times.AtLeastOnce);
            _writerMock.Verify(w => w.WriteByteString(null), Times.AtLeastOnce);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                ClientDescription = new ClientDescription(),
                SessionName = "order-string",
                RequestedSessionTimeout = 9999.9,
                MaxResponseMessageSize = 777
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteString("order-string")).Callback(() => callOrder.Add("Name"));
            _writerMock.Setup(w => w.WriteDouble(9999.9)).Callback(() => callOrder.Add("Timeout"));
            _writerMock.Setup(w => w.WriteUInt32(777)).Callback(() => callOrder.Add("Size"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int nameIdx = callOrder.IndexOf("Name");
            int timeoutIdx = callOrder.IndexOf("Timeout");
            int sizeIdx = callOrder.IndexOf("Size");

            Assert.True(nameIdx < timeoutIdx);
            Assert.True(timeoutIdx < sizeIdx);
        }
    }
}