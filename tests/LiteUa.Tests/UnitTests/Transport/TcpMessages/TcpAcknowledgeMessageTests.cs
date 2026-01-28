using LiteUa.Encoding;
using LiteUa.Transport.TcpMessages;
using Moq;

namespace LiteUa.Tests.UnitTests.Transport.TcpMessages
{
    [Trait("Category", "Unit")]
    public class TcpAcknowledgeMessageTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public TcpAcknowledgeMessageTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidStream_SetsAllPropertiesCorrectly()
        {
            // Arrange
            uint expectedVersion = 0;
            uint expectedRecvBuf = 65535;
            uint expectedSendBuf = 65535;
            uint expectedMaxMsg = 1048576;
            uint expectedMaxChunks = 100;

            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(expectedVersion)
                .Returns(expectedRecvBuf)
                .Returns(expectedSendBuf)
                .Returns(expectedMaxMsg)
                .Returns(expectedMaxChunks);

            var msg = new TcpAcknowledgeMessage();

            // Act
            msg.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(expectedVersion, msg.ProtocolVersion);
            Assert.Equal(expectedRecvBuf, msg.ReceiveBufferSize);
            Assert.Equal(expectedSendBuf, msg.SendBufferSize);
            Assert.Equal(expectedMaxMsg, msg.MaxMessageSize);
            Assert.Equal(expectedMaxChunks, msg.MaxChunkCount);
        }

        [Fact]
        public void Decode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var msg = new TcpAcknowledgeMessage();
            var callOrder = new List<string>();
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(() => { callOrder.Add("Protocol"); return 1u; })
                .Returns(() => { callOrder.Add("RecvBuf"); return 2u; })
                .Returns(() => { callOrder.Add("SendBuf"); return 3u; })
                .Returns(() => { callOrder.Add("MaxMsg"); return 4u; })
                .Returns(() => { callOrder.Add("MaxChunks"); return 5u; });

            // Act
            msg.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("Protocol", callOrder[0]);
            Assert.Equal("RecvBuf", callOrder[1]);
            Assert.Equal("SendBuf", callOrder[2]);
            Assert.Equal("MaxMsg", callOrder[3]);
            Assert.Equal("MaxChunks", callOrder[4]);
            Assert.Equal(1u, msg.ProtocolVersion);
            Assert.Equal(5u, msg.MaxChunkCount);
        }

        [Fact]
        public void Decode_IncompleteStream_PropagatesException()
        {
            // Arrange
            // Simulate a stream that ends after the 3rd UInt32
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)
                .Returns(65535u)
                .Returns(65535u)
                .Throws(new EndOfStreamException());

            var msg = new TcpAcknowledgeMessage();

            // Act & Assert
            Assert.Throws<EndOfStreamException>(() => msg.Decode(_readerMock.Object));
        }
    }
}