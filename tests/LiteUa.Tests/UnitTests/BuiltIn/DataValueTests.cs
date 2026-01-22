using LiteUa.BuiltIn;
using LiteUa.Encoding;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.BuiltIn
{
    [Trait("Category", "Unit")]
    public class DataValueTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public DataValueTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var dataValue = new DataValue();
            // Assert
            Assert.Null(dataValue.Value);
            Assert.Equal(new StatusCode(0), dataValue.StatusCode); // 0 = Good
            Assert.Equal(default, dataValue.SourceTimestamp);
            Assert.Equal(default, dataValue.ServerTimestamp);
        }

        [Fact]
        public void Encode_OnlyValuePresent_WritesCorrectMaskAndValue()
        {
            // Arrange
            var dataValue = new DataValue
            {
                Value = new Variant((short)42, BuiltInType.Int16)
            };
            // Act
            dataValue.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteByte(0x03), Times.Once); // Mask: 0x01 (Value) | 0x02 (StatusCode)
            _writerMock.Verify(w => w.WriteInt16(It.Is<short>(v => v == 42)), Times.Once);

        }

        [Fact]
        public void Decode_FieldsPresent_ReadsData()
        {
            // Arrange
            // Mask: 0x02 (Stat) | 0x04 (SrcTime) | 0x08 (SrvTime)
            // Total Mask = E
            byte mask = 0xE;
            var expectedStatusCode = new StatusCode(0x80000000);
            var expectedSourceTime = new DateTime(2023, 1, 1);
            var expectedServerTime = new DateTime(2023, 1, 2);

            _readerMock.Setup(r => r.ReadByte()).Returns(mask);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(expectedStatusCode.Code);
            _readerMock.SetupSequence(r => r.ReadDateTime())
                .Returns(expectedSourceTime)
                .Returns(expectedServerTime);

            // Act
            var result = DataValue.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(expectedSourceTime, result.SourceTimestamp);
            Assert.Equal(expectedServerTime, result.ServerTimestamp);
            Assert.Equal(expectedStatusCode, result.StatusCode);
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var dv = new DataValue() 
            { 
                Value = new Variant((short)123, BuiltInType.Int16),
                SourceTimestamp = new DateTime(2024, 1, 1),
                StatusCode = new StatusCode(2150891520)
            };

            // Act
            var str = dv.ToString();

            // Assert
            Assert.Contains("Value=", str);
            Assert.Contains(dv.Value.Value?.ToString()!, str);
            Assert.Contains("StatusCode=", str);
            Assert.Contains(dv.StatusCode.ToString(), str);
            Assert.Contains("SourceTimestamp=", str);
            Assert.Contains(dv.SourceTimestamp.ToString(), str);
        }
    }
}
