using LiteUa.BuiltIn;
using LiteUa.Encoding;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Client.BuiltIn
{
    public class StatusCodeTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public StatusCodeTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Theory]
        [InlineData(0x00000000, true, false)]  // Good
        [InlineData(0x00010000, true, false)]  // Good_CompletesAsynchronously
        [InlineData(0x80010000, false, true)]  // Bad_UnexpectedError
        [InlineData(0x80050000, false, true)]  // Bad_Timeout
        [InlineData(0x40000000, true, false)]  // Uncertain (Bit 31 is 0, so IsGood is true)
        [InlineData(0xFFFFFFFF, false, true)]  // Maximum value (Bad)
        public void SeverityProperties_ReturnCorrectBoolean(uint code, bool expectedGood, bool expectedBad)
        {
            // Arrange
            var status = new StatusCode(code);

            // Assert
            Assert.Equal(expectedGood, status.IsGood);
            Assert.Equal(expectedBad, status.IsBad);
        }

        [Fact]
        public void Encode_WritesUintToStream()
        {
            // Arrange
            uint expectedCode = 0x80010000;
            var status = new StatusCode(expectedCode);

            // Act
            status.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt32(expectedCode), Times.Once);
        }

        [Fact]
        public void Decode_ReadsUintFromStream()
        {
            // Arrange
            uint incomingCode = 0x00000000;
            _readerMock.Setup(r => r.ReadUInt32()).Returns(incomingCode);

            // Act
            var result = StatusCode.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(incomingCode, result.Code);
        }

        [Theory]
        [InlineData(0, "0x00000000")]
        [InlineData(0x80010000, "0x80010000")]
        [InlineData(255, "0x000000FF")]
        public void ToString_FormatsAsHex(uint code, string expectedString)
        {
            // Arrange
            var status = new StatusCode(code);

            // Act
            var result = status.ToString();

            // Assert
            Assert.Equal(expectedString, result);
        }

        [Fact]
        public void CodeProperty_IsMutable()
        {
            // Arrange
            var status = new StatusCode(0);

            // Act
            status.Code = 0x80000000;

            // Assert
            Assert.Equal(0x80000000u, status.Code);
            Assert.True(status.IsBad);
        }
    }
}
