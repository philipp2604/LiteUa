using LiteUa.BuiltIn;
using LiteUa.Encoding;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Client.BuiltIn
{
    public class ExtensionObjectTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public ExtensionObjectTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Decode_NoBody_ReturnsExtensionObjectWithZeroEncoding()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0x00); // NodeId Type 0 (TwoByte)
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0)    // NodeId identifier
                .Returns(0x00); // ExtensionObject Encoding Mask (None)

            // Act
            var result = ExtensionObject.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(0x00, result.Encoding);
            Assert.Null(result.Body);
            Assert.Null(result.DecodedValue);
        }

        [Fact]
        public void Decode_RawByteString_NoRegistryMatch_ReturnsRawBytes()
        {
            // Arrange
            byte[] rawData = [0xAA, 0xBB, 0xCC];
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0x00) // NodeId type
                .Returns(10)   // NodeId identifier
                .Returns(0x01); // Encoding: ByteString

            _readerMock.Setup(r => r.ReadInt32()).Returns(rawData.Length);
            _readerMock.Setup(r => r.ReadBytes(rawData.Length)).Returns(rawData);

            // Act
            var result = ExtensionObject.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(0x01, result.Encoding);
            Assert.Equal(rawData, result.Body);
            Assert.Null(result.DecodedValue); // No registry match assumed
        }

        [Fact]
        public void Encode_RawBytes_WritesCorrectStructure()
        {
            // Arrange
            var eo = new ExtensionObject
            {
                TypeId = new NodeId(0, 100),
                Encoding = 0x01,
                Body = [0x01, 0x02]
            };

            // Act
            eo.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteByte(0x01), Times.AtLeastOnce); // Encoding Mask
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once);          // Length
            _writerMock.Verify(w => w.WriteBytes(eo.Body), Times.Once);
        }

        [Fact]
        public void Encode_TypedObject_NotRegistered_ThrowsException()
        {
            // Arrange
            var eo = new ExtensionObject
            {
                DecodedValue = new { Name = "UnknownType" } // An anonymous type that won't be in registry
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => eo.Encode(_writerMock.Object));
        }

        [Fact]
        public void ToString_ReturnsValidFormat()
        {
            // Arrange
            var eo = new ExtensionObject
            {
                TypeId = new NodeId(1, "MyType"),
                Encoding = 0x01,
                Body = new byte[10]
            };

            // Act
            var str = eo.ToString();

            // Assert
            Assert.Contains("TypeId=", str);
            Assert.Contains("Encoding=0x01", str);
            Assert.Contains("BodyLength=10", str);
        }

        [Fact]
        public void Decode_LengthNegativeOne_BodyIsNull()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0x00); // NodeId
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(1)    // NodeId ID
                .Returns(0x01); // ByteString

            _readerMock.Setup(r => r.ReadInt32()).Returns(-1); // OPC UA Null ByteString

            // Act
            var result = ExtensionObject.Decode(_readerMock.Object);

            // Assert
            Assert.Null(result.Body);
        }
    }
}
