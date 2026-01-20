using LiteUa.Encoding;
using LiteUa.Transport.TcpMessages;
using Moq;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Transport.TcpMessages
{
    [Trait("Category", "Unit")]
    public class TcpHelloMessageTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public TcpHelloMessageTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_NullUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TcpHelloMessage(null!));
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var msg = new TcpHelloMessage("opc.tcp://localhost:4840");

            // Assert
            Assert.Equal(0u, msg.ProtocolVersion);
            Assert.Equal(0xFFFFu, msg.ReceiveBufferSize);
            Assert.Equal(0xFFFFu, msg.SendBufferSize);
            Assert.Equal(0u, msg.MaxMessageSize);
            Assert.Equal(0u, msg.MaxChunkCount);
        }

        [Fact]
        public void Encode_WritesCorrectStaticHeader()
        {
            // Arrange
            var msg = new TcpHelloMessage("opc.tcp://localhost");

            // Act
            msg.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteBytes(It.Is<byte[]>(b => b[0] == (byte)'H' && b[1] == (byte)'E' && b[2] == (byte)'L')), Times.Once);
            _writerMock.Verify(w => w.WriteByte((byte)'F'), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var msg = new TcpHelloMessage("url-marker")
            {
                ProtocolVersion = 111,
                MaxChunkCount = 222
            };
            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteUInt32(111)).Callback(() => callOrder.Add("Version"));
            _writerMock.Setup(w => w.WriteUInt32(222)).Callback(() => callOrder.Add("MaxChunks"));
            _writerMock.Setup(w => w.WriteString("url-marker")).Callback(() => callOrder.Add("Url"));

            // Act
            msg.Encode(_writerMock.Object);

            // Assert
            int verIdx = callOrder.IndexOf("Version");
            int chunkIdx = callOrder.IndexOf("MaxChunks");
            int urlIdx = callOrder.IndexOf("Url");

            Assert.True(verIdx < chunkIdx);
            Assert.True(chunkIdx < urlIdx);
        }

        [Fact]
        public void Encode_PerformsLengthPatchingCorrectly()
        {
            // Arrange
            var url = "opc.tcp://127.0.0.1:4840";
            var msg = new TcpHelloMessage(url);

            using var ms = new MemoryStream();
            var writer = new OpcUaBinaryWriter(ms);

            // Act
            msg.Encode(writer);

            // Assert
            byte[] buffer = ms.ToArray();
            uint expectedTotalLength = (uint)buffer.Length;

            // Header: HEL (3) + F (1) = 4 bytes offset
            uint actualPatchedLength = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(4, 4));
            Assert.Equal(expectedTotalLength, actualPatchedLength);

            string messageType = System.Text.Encoding.ASCII.GetString(buffer, 0, 3);
            Assert.Equal("HEL", messageType);
            Assert.Equal((byte)'F', buffer[3]);
        }

        [Fact]
        public void Encode_WritesCorrectBodyValues()
        {
            // Arrange
            var url = "opc.tcp://localhost";
            var msg = new TcpHelloMessage(url)
            {
                ProtocolVersion = 1,
                ReceiveBufferSize = 8192,
                SendBufferSize = 4096
            };

            using var ms = new MemoryStream();
            var writer = new OpcUaBinaryWriter(ms);

            // Act
            msg.Encode(writer);
            byte[] buffer = ms.ToArray();

            // Assert - HEL(3) + F(1) + Size(4) = 8 bytes
            uint protocol = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(8, 4));
            uint recvBuf = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(12, 4));
            uint sendBuf = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(16, 4));

            Assert.Equal(1u, protocol);
            Assert.Equal(8192u, recvBuf);
            Assert.Equal(4096u, sendBuf);
        }

        [Fact]
        public void Encode_WritesCorrectUrl()
        {
            // Arrange
            string url = "opc.tcp://test-server:4840";
            var msg = new TcpHelloMessage(url);
            using var ms = new MemoryStream();
            var writer = new OpcUaBinaryWriter(ms);

            // Act
            msg.Encode(writer);
            byte[] buffer = ms.ToArray();
            int urlLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(buffer.Length - url.Length - 4, 4));
            string actualUrl = System.Text.Encoding.UTF8.GetString(buffer, buffer.Length - url.Length, url.Length);

            // Assert
            Assert.Equal(url.Length, urlLength);
            Assert.Equal(url, actualUrl);
        }
    }
}
