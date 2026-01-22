using LiteUa.BuiltIn;
using LiteUa.Encoding;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.BuiltIn
{
    [Trait("Category", "Unit")]
    public class ExpandedNodeIdTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public ExpandedNodeIdTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new System.IO.MemoryStream());
            _writerMock = new Mock<OpcUaBinaryWriter>(new System.IO.MemoryStream());
        }

        #region Decode Tests

        [Fact]
        public void Decode_TwoByteNodeId_MinimalFlags_SetsProperties()
        {
            // Arrange
            // Type 0 (TwoByte), no extra flags
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0x00) // Encoding byte
                .Returns(55); // Identifier

            // Act
            var result = ExpandedNodeId.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(0u, result.NodeId.NamespaceIndex);
            Assert.Equal(55u, result.NodeId.NumericIdentifier);
            Assert.Null(result.NamespaceUri);
        }

        [Fact]
        public void Decode_StringNodeId_WithAllFlags_ReadsFullStructure()
        {
            // Arrange
            // Mask: 0x80 (NamespaceUri) | 0x40 (ServerIndex) | 0x03 (String Type) = 0xC3
            _readerMock.Setup(r => r.ReadByte()).Returns(0xC3);
            _readerMock.Setup(r => r.ReadUInt16()).Returns(2); // NamespaceIndex
            _readerMock.SetupSequence(r => r.ReadString()).Returns("MyNode").Returns("http://myuri.org");
            _readerMock.Setup(r => r.ReadUInt32()).Returns(100u); // ServerIndex

            // Act
            var result = ExpandedNodeId.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(2, result.NodeId.NamespaceIndex);
            Assert.Equal("MyNode", result.NodeId.StringIdentifier);
            Assert.Equal("http://myuri.org", result.NamespaceUri);
            Assert.Equal(100u, result.ServerIndex);
        }

        [Theory]
        [InlineData(0x01)] // FourByte
        [InlineData(0x02)] // Numeric
        [InlineData(0x04)] // Guid
        [InlineData(0x05)] // ByteString
        public void Decode_DifferentNodeIdTypes_CallsCorrectReaderMethods(byte typeByte)
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(typeByte);

            // Act
            var result = ExpandedNodeId.Decode(_readerMock.Object);

            // Assert
            // Verify specific methods were called based on type
            if (typeByte == 0x04) _readerMock.Verify(r => r.ReadGuid(), Times.Once);
            if (typeByte == 0x05) _readerMock.Verify(r => r.ReadByteString(), Times.Once);
        }

        #endregion

        #region Encode Tests

        [Fact]
        public void Encode_TwoByteNodeId_WritesCorrectBytes()
        {
            // Arrange
            var eni = new ExpandedNodeId
            {
                NodeId = new NodeId(0, 10) // Small numeric, NS 0
            };

            // Act
            eni.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteByte(0x00), Times.Once); // Encoding byte
            _writerMock.Verify(w => w.WriteByte(10), Times.Once);   // Identifier
        }

        [Fact]
        public void Encode_FourByteNodeId_WritesCorrectBytes()
        {
            // Arrange
            var eni = new ExpandedNodeId
            {
                NodeId = new NodeId(5, 1000) // NS < 255, ID < 65535
            };

            // Act
            eni.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteByte(0x01), Times.Once);
            _writerMock.Verify(w => w.WriteByte(5), Times.Once);
            _writerMock.Verify(w => w.WriteUInt16(1000), Times.Once);
        }

        [Fact]
        public void Encode_StringNodeId_WithNamespaceUri_SetsHighBit()
        {
            // Arrange
            var eni = new ExpandedNodeId
            {
                NodeId = new NodeId(1, "MyString"),
                NamespaceUri = "http://example.org"
            };

            // Act
            eni.Encode(_writerMock.Object);

            // Assert
            // 0x80 (Uri Flag) | 0x03 (String Type) = 0x83
            _writerMock.Verify(w => w.WriteByte(0x83), Times.Once);
            _writerMock.Verify(w => w.WriteString("MyString"), Times.Once);
            _writerMock.Verify(w => w.WriteString("http://example.org"), Times.Once);
        }

        [Fact]
        public void Encode_FullExpandedNodeId_WritesAllComponents()
        {
            // Arrange
            var eni = new ExpandedNodeId
            {
                NodeId = new NodeId(1, 999999u), // Numeric Type (2)
                NamespaceUri = "uri",
                ServerIndex = 50
            };

            // Act
            eni.Encode(_writerMock.Object);

            // Assert
            // 0x80 (Uri) | 0x40 (Server) | 0x02 (Numeric) = 0xC2
            _writerMock.Verify(w => w.WriteByte(0xC2), Times.Once);
            _writerMock.Verify(w => w.WriteString("uri"), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(50), Times.Once);
        }

        [Fact]
        public void Encode_NullNodeId_ThrowsInvalidOperationException()
        {
            // Arrange
            var eni = new ExpandedNodeId { NodeId = null! };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => eni.Encode(_writerMock.Object));
        }

        #endregion

        [Fact]
        public void ToString_ReturnsNodeIdString()
        {
            // Arrange
            var eni = new ExpandedNodeId { NodeId = new NodeId(1, "Test") };

            // Act
            var result = eni.ToString();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ns=1;s=Test", result);
        }
    }
}
