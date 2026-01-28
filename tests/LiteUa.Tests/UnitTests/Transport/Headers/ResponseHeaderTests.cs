using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using Moq;

namespace LiteUa.Tests.UnitTests.Transport.Headers
{
    [Trait("Category", "Unit")]
    public class ResponseHeaderTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public ResponseHeaderTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidStream_SetsStandardProperties()
        {
            // Arrange
            var expectedTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            uint expectedHandle = 42;
            uint expectedResult = 0x80010000; // Bad_UnexpectedError

            _readerMock.Setup(r => r.ReadDateTime()).Returns(expectedTime);
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(expectedHandle)
                .Returns(expectedResult);
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0) // DiagnosticInfo mask
                .Returns(0) // ExtensionObject (AdditionalHeader) NodeId Type
                .Returns(0) // ExtensionObject (AdditionalHeader) NodeId ID
                .Returns(0); // ExtensionObject (AdditionalHeader) Encoding Mask
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);

            // Act
            var result = ResponseHeader.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(expectedTime, result.Timestamp);
            Assert.Equal(expectedHandle, result.RequestHandle);
            Assert.Equal(expectedResult, result.ServiceResult);
            Assert.Null(result.ServiceDiagnostics);
            Assert.NotNull(result.StringTable);
            Assert.Empty(result.StringTable);
            Assert.NotNull(result.AdditionalHeader);
        }

        [Fact]
        public void Decode_WithStrings_PopulatesStringTable()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // StringTable Count = 2
            _readerMock.Setup(r => r.ReadInt32()).Returns(2);
            _readerMock.SetupSequence(r => r.ReadString())
                .Returns("ErrorDetail1")
                .Returns("ErrorDetail2");

            // Act
            var result = ResponseHeader.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result.StringTable);
            Assert.Equal(2, result.StringTable.Length);
            Assert.Equal("ErrorDetail1", result.StringTable[0]);
            Assert.Equal("ErrorDetail2", result.StringTable[1]);
        }

        [Fact]
        public void Decode_HandlesNullStringTable_WhenCountIsNegativeOne()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadInt32()).Returns(-1);

            // Act
            var result = ResponseHeader.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result.StringTable);
            Assert.Empty(result.StringTable);
        }

        [Fact]
        public void Decode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadDateTime()).Returns(() =>
            {
                callOrder.Add("Time");
                return DateTime.MinValue;
            });

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() =>
            {
                callOrder.Add("UInt32");
                return 0u;
            });

            _readerMock.Setup(r => r.ReadByte()).Returns(() =>
            {
                callOrder.Add("Byte");
                return 0;
            });

            _readerMock.Setup(r => r.ReadInt32()).Returns(() =>
            {
                callOrder.Add("Int32");
                return 0;
            });

            // Act
            ResponseHeader.Decode(_readerMock.Object);

            // Assert
            // Time -> Handle -> Result -> Diags(Byte) -> StringTable(Int32) -> AdditionalHeader(Byte)
            Assert.Equal("Time", callOrder[0]);
            Assert.Equal("UInt32", callOrder[1]); // RequestHandle
            Assert.Equal("UInt32", callOrder[2]); // ServiceResult
            Assert.Equal("Byte", callOrder[3]); // ServiceDiagnostics mask
            Assert.Equal("Int32", callOrder[4]); // StringTable count
            Assert.Equal("Byte", callOrder[5]); // AdditionalHeader start
        }
    }
}