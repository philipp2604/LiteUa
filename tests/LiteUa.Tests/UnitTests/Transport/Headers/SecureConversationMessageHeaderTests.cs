using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Transport.Headers
{
    [Trait("Category", "Unit")]
    public class SecureConversationMessageHeaderTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public SecureConversationMessageHeaderTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("M")]
        [InlineData("MS")]
        [InlineData("MSGF")] // 4 chars
        public void Encode_InvalidMessageType_ThrowsArgumentException(string? invalidType)
        {
            // Arrange
            var header = new SecureConversationMessageHeader
            {
                MessageType = invalidType,
                ChunkType = 'F'
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => header.Encode(_writerMock.Object));
            Assert.Contains("MessageType must be 3 chars", ex.Message);
        }

        [Fact]
        public void Encode_ValidHeader_WritesCorrectBytes()
        {
            // Arrange
            var header = new SecureConversationMessageHeader
            {
                MessageType = "MSG",
                ChunkType = 'F',
                MessageSize = 1024,
                SecureChannelId = 88
            };

            // Act
            header.Encode(_writerMock.Object);

            // Assert
            // Verify "MSG"
            _writerMock.Verify(w => w.WriteByte((byte)'M'), Times.Once);
            _writerMock.Verify(w => w.WriteByte((byte)'S'), Times.Once);
            _writerMock.Verify(w => w.WriteByte((byte)'G'), Times.Once);

            // Verify "F"
            _writerMock.Verify(w => w.WriteByte((byte)'F'), Times.Once);

            // Verify UInt32s
            _writerMock.Verify(w => w.WriteUInt32(1024u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(88u), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesExactFieldOrder()
        {
            // Arrange
            var header = new SecureConversationMessageHeader
            {
                MessageType = "OPN",
                ChunkType = 'C',
                MessageSize = 100,
                SecureChannelId = 200
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteByte(It.IsAny<byte>()))
                       .Callback((byte b) => callOrder.Add($"Byte:{(char)b}"));

            _writerMock.Setup(w => w.WriteUInt32(It.IsAny<uint>()))
                       .Callback((uint u) => callOrder.Add($"UInt32:{u}"));

            // Act
            header.Encode(_writerMock.Object);

            // Assert
            // MessageType (3 bytes) -> ChunkType (1 byte) -> MessageSize (UInt32) -> SecureChannelId (UInt32)
            Assert.Equal("Byte:O", callOrder[0]);
            Assert.Equal("Byte:P", callOrder[1]);
            Assert.Equal("Byte:N", callOrder[2]);
            Assert.Equal("Byte:C", callOrder[3]);
            Assert.Equal("UInt32:100", callOrder[4]);
            Assert.Equal("UInt32:200", callOrder[5]);
        }

        [Fact]
        public void Properties_AreMutable()
        {
            // Act
            var header = new SecureConversationMessageHeader
            {
                MessageType = "CLO",
                ChunkType = 'A',
                MessageSize = 50,
                SecureChannelId = 1
            };

            // Assert
            Assert.Equal("CLO", header.MessageType);
            Assert.Equal('A', header.ChunkType);
            Assert.Equal(50u, header.MessageSize);
            Assert.Equal(1u, header.SecureChannelId);
        }
    }
}
