using LiteUa.Encoding;
using LiteUa.Stack.Session;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Session
{
    [Trait("Category", "Unit")]
    public class ActivateSessionResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public ActivateSessionResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(470u, ActivateSessionResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_BasicResponse_SetsProperties()
        {
            // Arrange
            var expectedNonce = new byte[] { 0xAA, 0xBB, 0xCC };
            _readerMock.Setup(r => r.ReadByteString()).Returns(expectedNonce);
            _readerMock.Setup(r => r.ReadInt32()).Returns(1); // One result

            // Skip DiagnosticInfo block
            _readerMock.Setup(r => r.Position).Returns(100);
            _readerMock.Setup(r => r.Length).Returns(100);

            // Act
            var response = new ActivateSessionResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ResponseHeader);
            Assert.Equal(expectedNonce, response.ServerNonce);
            Assert.NotNull(response.Results);
            Assert.Single(response.Results);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_WithDiagnostics_ParsesAllFields()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0); // Masks for Header and Diags

            // 1. Header StringTable (0)
            // 2. Results Count (1)
            // 3. Diagnostics Count (1)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header
                .Returns(1) // Results
                .Returns(1); // Diagnostics

            // trigger optional diags
            _readerMock.Setup(r => r.Position).Returns(0);
            _readerMock.Setup(r => r.Length).Returns(500);

            // Act
            var response = new ActivateSessionResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Single(response.Results!);
            Assert.Single(response.DiagnosticInfos!);
        }

        [Fact]
        public void Decode_EmptyResults_HandlesCountZero()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Header StringTable (0), then Results Count (0)
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);

            // Block diagnostics
            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new ActivateSessionResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Null(response.Results);
        }

        [Fact]
        public void Decode_HandlesNullResults_WhenCountIsNegativeOne()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Header StringTable (0), then Results Count (-1)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(-1);

            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new ActivateSessionResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Null(response.Results);
        }

        [Fact]
        public void Decode_SequenceCheck_VerifiesFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            // Header reads
            _readerMock.Setup(r => r.ReadInt64()).Returns(0);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            _readerMock.Setup(r => r.ReadByteString()).Returns(() =>
            {
                callOrder.Add("Nonce");
                return null;
            });

            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header StringTable
                .Returns(() =>
                {
                    callOrder.Add("ResultsCount");
                    return 0;
                });

            // Act
            var response = new ActivateSessionResponse();
            response.Decode(_readerMock.Object);

            // Assert
            // In the sequence: Nonce (ByteString) -> ResultsCount (Int32)
            Assert.Equal("Nonce", callOrder[0]);
            Assert.Equal("ResultsCount", callOrder[1]);
        }
    }
}