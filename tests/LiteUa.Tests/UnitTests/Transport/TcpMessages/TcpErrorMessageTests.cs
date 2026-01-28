using LiteUa.Encoding;
using LiteUa.Transport.TcpMessages;
using Moq;

namespace LiteUa.Tests.UnitTests.Transport.TcpMessages
{
    [Trait("Category", "Unit")]
    public class TcpErrorMessageTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public TcpErrorMessageTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidError_SetsProperties()
        {
            // Arrange
            // Bad_TcpMessageTypeInvalid = 0x80010000
            uint expectedError = 0x80010000;
            string expectedReason = "Invalid message type received.";

            _readerMock.Setup(r => r.ReadUInt32()).Returns(expectedError);
            _readerMock.Setup(r => r.ReadString()).Returns(expectedReason);

            var msg = new TcpErrorMessage();

            // Act
            msg.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(expectedError, msg.ErrorCode);
            Assert.Equal(expectedReason, msg.Reason);
        }

        [Fact]
        public void Decode_NullReason_HandlesCorrectly()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0x80050000u); // Bad_Timeout
            _readerMock.Setup(r => r.ReadString()).Returns((string?)null); // Null string

            var msg = new TcpErrorMessage();

            // Act
            msg.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(0x80050000u, msg.ErrorCode);
            Assert.Null(msg.Reason);
        }

        [Fact]
        public void Decode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var msg = new TcpErrorMessage();
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() =>
            {
                callOrder.Add("ErrorCode");
                return 0u;
            });

            _readerMock.Setup(r => r.ReadString()).Returns(() =>
            {
                callOrder.Add("Reason");
                return "msg";
            });

            // Act
            msg.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("ErrorCode", callOrder[0]);
            Assert.Equal("Reason", callOrder[1]);
        }

        [Fact]
        public void Decode_TruncatedStream_PropagatesException()
        {
            // Arrange: Stream ends before the string part
            _readerMock.Setup(r => r.ReadUInt32()).Returns(1u);
            _readerMock.Setup(r => r.ReadString()).Throws(new EndOfStreamException());

            var msg = new TcpErrorMessage();

            // Act & Assert
            Assert.Throws<EndOfStreamException>(() => msg.Decode(_readerMock.Object));
        }
    }
}