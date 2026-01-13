using LiteUa.BuiltIn;
using LiteUa.Encoding;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Client.BuiltIn
{
    public class VariantTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public VariantTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_IntArray_SetsIsArrayTrue()
        {
            // Arrange
            var val = new int[] { 1, 2, 3 };

            // Act
            var variant = new Variant(val, BuiltInType.Int32);

            // Assert
            Assert.True(variant.IsArray);
            Assert.Equal(BuiltInType.Int32, variant.Type);
        }

        [Fact]
        public void Constructor_ByteString_SetsIsArrayFalse()
        {
            // Arrange
            // A ByteString in OPC UA is a byte[], but it is treated as a SCALAR, not an array of bytes.
            var val = new byte[] { 0x01, 0x02 };

            // Act
            var variant = new Variant(val, BuiltInType.ByteString);

            // Assert
            Assert.False(variant.IsArray, "ByteString should be treated as a scalar ByteString, not an array of Bytes.");
        }

        [Fact]
        public void Constructor_ByteArray_SetsIsArrayTrue()
        {
            // Arrange
            var val = new byte[] { 0x01, 0x02 };

            // Act
            var variant = new Variant(val, BuiltInType.Byte);

            // Assert
            Assert.True(variant.IsArray, "When type is Byte, a byte[] should be treated as an array.");
        }

        #endregion

        #region Encode Tests

        [Fact]
        public void Encode_ScalarInt32_WritesCorrectMaskAndValue()
        {
            // Arrange
            var variant = new Variant(42, BuiltInType.Int32);

            // Act
            variant.Encode(_writerMock.Object);

            // Assert
            // Mask for Int32 is 6 (0x06). No flags set.
            _writerMock.Verify(w => w.WriteByte(0x06), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(42), Times.Once);
        }

        [Fact]
        public void Encode_StringArray_WritesArrayFlagAndLength()
        {
            // Arrange
            var strings = new string[] { "A", "B" };
            var variant = new Variant(strings, BuiltInType.String);

            // Act
            variant.Encode(_writerMock.Object);

            // Assert
            // Mask: String (12 / 0x0C) | Array Flag (0x80) = 0x8C
            _writerMock.Verify(w => w.WriteByte(0x8C), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once); // Length
            _writerMock.Verify(w => w.WriteString("A"), Times.Once);
            _writerMock.Verify(w => w.WriteString("B"), Times.Once);
        }

        [Fact]
        public void Encode_WithDimensions_WritesDimensionFlagAndDimensions()
        {
            // Arrange
            var data = new int[] { 100, 101, 102, 103, 104, 105 }; // Length 6
            var dims = new int[] { 2, 3 }; // 2x3 matrix, total 6 elements

            var variant = new Variant(data, BuiltInType.Int32)
            {
                ArrayDimensions = dims
            };

            // Act
            variant.Encode(_writerMock.Object);

            // Assert
            // 1. Verify Mask: Int32 (6) | Array (0x80) | Dimensions (0x40) = 0xC6 (198)
            _writerMock.Verify(w => w.WriteByte(0xC6), Times.Once);

            // 2. Verify Array Metadata
            _writerMock.Verify(w => w.WriteInt32(6), Times.Once); // The length of the array

            // 3. Verify Data
            _writerMock.Verify(w => w.WriteInt32(100), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(105), Times.Once);

            // 4. Verify Dimension Metadata
            // WriteInt32(2) should happen twice: 
            // - Once for the number of dimensions (dims.Length = 2)
            // - Once for the first dimension size (dims[0] = 2)
            _writerMock.Verify(w => w.WriteInt32(2), Times.Exactly(2));

            // WriteInt32(3) should happen once:
            // - For the second dimension size (dims[1] = 3)
            _writerMock.Verify(w => w.WriteInt32(3), Times.Once);
        }

        #endregion

        #region Decode Tests

        [Fact]
        public void Decode_ScalarBoolean_ReturnsVariant()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0x01); // Boolean (1)
            _readerMock.Setup(r => r.ReadBoolean()).Returns(true);

            // Act
            var result = Variant.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(BuiltInType.Boolean, result.Type);
            Assert.False(result.IsArray);
            Assert.Equal(true, result.Value);
        }

        [Fact]
        public void Decode_Int32Array_ReturnsVariantWithArray()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0x86); // Array (0x80) | Int32 (0x06)
            _readerMock.Setup(r => r.ReadInt32()).Returns(2); // Length
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(2) // The length
                .Returns(100) // Element 0
                .Returns(200); // Element 1

            // Act
            var result = Variant.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.IsArray);
            var arr = Assert.IsType<int[]>(result.Value);
            Assert.Equal(2, arr.Length);
            Assert.Equal(100, arr[0]);
            Assert.Equal(200, arr[1]);
        }

        [Fact]
        public void Decode_RecursiveVariant_HandlesNestedVariant()
        {
            // Arrange
            // Outer Mask: 0x18 (24 = Variant)
            // Inner Mask: 0x0C (12 = String)
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0x18) // Outer Variant Type
                .Returns(0x0C); // Inner String Type

            _readerMock.Setup(r => r.ReadString()).Returns("Nested Content");

            // Act
            var result = Variant.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(BuiltInType.Variant, result.Type);
            var inner = Assert.IsType<Variant>(result.Value);
            Assert.Equal(BuiltInType.String, inner.Type);
            Assert.Equal("Nested Content", inner.Value);
        }

        #endregion

        [Fact]
        public void ToString_Array_ReturnsFormattedString()
        {
            // Arrange
#pragma warning disable CA1861
            var variant = new Variant(new double[] { 1.1, 2.2 }, BuiltInType.Double);
#pragma warning restore CA1861

            // Act
            var str = variant.ToString();

            // Assert
            Assert.Equal("Double[2]", str);
        }
    }
}
