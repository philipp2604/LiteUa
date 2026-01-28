using LiteUa.BuiltIn;
using LiteUa.Encoding;
using Moq;

namespace LiteUa.Tests.UnitTests.BuiltIn
{
    [Trait("Category", "Unit")]
    public class DiagnosticInfoTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public DiagnosticInfoTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new System.IO.MemoryStream());
            _writerMock = new Mock<OpcUaBinaryWriter>(new System.IO.MemoryStream());
        }

        [Fact]
        public void Decode_MaskZero_ReturnsNull()
        {
            // Arrange: Mask 0x00 means no DiagnosticInfo
            _readerMock.Setup(r => r.ReadByte()).Returns(0x00);

            // Act
            var result = DiagnosticInfo.Decode(_readerMock.Object);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Decode_SimpleFields_ParsesCorrectly()
        {
            // Arrange: Mask 0x01 | 0x10 (SymbolicId and AdditionalInfo)
            _readerMock.Setup(r => r.ReadByte()).Returns(0x11);
            _readerMock.Setup(r => r.ReadInt32()).Returns(42);
            _readerMock.Setup(r => r.ReadString()).Returns("Error Detail");

            // Act
            var result = DiagnosticInfo.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(42, result.SymbolicId);
            Assert.Equal("Error Detail", result.AdditionalInfo);
            Assert.Null(result.NamespaceUri); // Bit not set
        }

        [Fact]
        public void Decode_RecursiveDiagnosticInfo_ParsesNestedStructure()
        {
            // Arrange:
            // 1. Parent Mask: 0x40 (InnerDiagnosticInfo present)
            // 2. Child Mask: 0x01 (SymbolicId present)
            // 3. Grandchild Mask: 0x00 (Null/End recursion)

            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0x40) // Outer mask
                .Returns(0x01) // Inner mask
                .Returns(0x00); // Inner-Inner (terminator)

            _readerMock.Setup(r => r.ReadInt32()).Returns(999);

            // Act
            var result = DiagnosticInfo.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.InnerDiagnosticInfo);
            Assert.Equal(999, result.InnerDiagnosticInfo.SymbolicId);
            Assert.Null(result.InnerDiagnosticInfo.InnerDiagnosticInfo);
        }

        [Fact]
        public void Decode_AllFields_ParsesEverything()
        {
            // Arrange: Mask 0x3F (All bits except recursion)
            byte mask = 0x01 | 0x02 | 0x04 | 0x08 | 0x10 | 0x20;
            _readerMock.Setup(r => r.ReadByte()).Returns(mask);
            _readerMock.SetupSequence(r => r.ReadInt32()).Returns(1).Returns(2).Returns(3).Returns(4);
            _readerMock.Setup(r => r.ReadString()).Returns("Test");
            _readerMock.Setup(r => r.ReadUInt32()).Returns(500u);

            // Act
            var result = DiagnosticInfo.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(1, result?.SymbolicId);
            Assert.Equal(2, result?.NamespaceUri);
            Assert.Equal(3, result?.LocalizedText);
            Assert.Equal(4, result?.Locale);
            Assert.Equal("Test", result?.AdditionalInfo);
            Assert.Equal(500u, result?.InnerStatusCode);
        }

        [Fact]
        public void Encode_ThrowsNotImplementedException()
        {
            // Arrange
            var info = new DiagnosticInfo();

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => info.Encode(_writerMock.Object));
        }

        [Fact]
        public void ToString_ContainsRelevantInformation()
        {
            // Arrange
            var info = new DiagnosticInfo { AdditionalInfo = "Critical Failure", SymbolicId = 5 };

            // Act
            var result = info.ToString();

            // Assert
            Assert.Contains("Critical Failure", result);
            Assert.Contains("SymbolicId=5", result);
        }

        [Theory]
        [InlineData(0x01, true, false)] // SymbolicId only
        [InlineData(0x20, false, true)] // StatusCode only
        public void Decode_IndividualBits_SetsCorrectProperties(byte mask, bool expectSymbolic, bool expectStatus)
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(mask);
            _readerMock.Setup(r => r.ReadInt32()).Returns(10);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(200u);

            // Act
            var result = DiagnosticInfo.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(expectSymbolic, result?.SymbolicId.HasValue);
            Assert.Equal(expectStatus, result?.InnerStatusCode.HasValue);
        }
    }
}