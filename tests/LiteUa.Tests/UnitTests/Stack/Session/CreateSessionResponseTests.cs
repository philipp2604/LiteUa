using LiteUa.Encoding;
using LiteUa.Stack.Session;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Session
{
    [Trait("Category", "Unit")]
    public class CreateSessionResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public CreateSessionResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(464u, CreateSessionResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_BasicResponse_SetsAllProperties()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.Now);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(12345u); // Handle/Size
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadDouble()).Returns(60000.0);
            _readerMock.Setup(r => r.ReadByteString()).Returns([0xAA]);
            _readerMock.Setup(r => r.ReadString()).Returns("http://algo-uri");

            // Act
            var response = new CreateSessionResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ResponseHeader);
            Assert.NotNull(response.SessionId);
            Assert.NotNull(response.AuthenticationToken);
            Assert.Equal(60000.0, response.RevisedSessionTimeout);
            Assert.Equal([0xAA], response.ServerNonce);
            Assert.NotNull(response.ServerSignature);
            Assert.Equal("http://algo-uri", response.ServerSignature.Algorithm);
            Assert.Equal(12345u, response.MaxRequestMessageSize);
        }

        [Fact]
        public void Decode_SoftwareCertificatesPresent_ThrowsNotImplementedException()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Sequence: StringTable(0), Endpoints(0), SoftwareCerts(1)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(0)
                .Returns(1);

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => new CreateSessionResponse().Decode(_readerMock.Object));
        }

        [Fact]
        public void Decode_WithEndpoints_ParsesArray()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Sequence: Header(0), Endpoints(1), SoftwareCerts(0)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(1)
                .Returns(0);

            // EndpointDescription.Decode (Strings, ByteStrings, Mode, Level)
            _readerMock.Setup(r => r.ReadString()).Returns("opc.tcp://url");
            _readerMock.Setup(r => r.ReadByteString()).Returns([]);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);

            // Act
            var response = new CreateSessionResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ServerEndpoints);
            Assert.Single(response.ServerEndpoints);
            Assert.Equal("opc.tcp://url", response.ServerEndpoints[0].EndpointUrl);
        }

        [Fact]
        public void Decode_SequenceCheck_VerifiesFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadDouble()).Returns(() =>
            {
                callOrder.Add("Timeout");
                return 0;
            });

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() =>
            {
                callOrder.Add("MaxMsgSize");
                return 0;
            });

            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.Now);
            _readerMock.Setup(r => r.ReadByteString()).Returns([]);
            _readerMock.Setup(r => r.ReadString()).Returns("");
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);

            // Act
            var response = new CreateSessionResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Contains("Timeout", callOrder);
            Assert.Contains("MaxMsgSize", callOrder);
            Assert.Equal("MaxMsgSize", callOrder.Last());
        }
    }
}