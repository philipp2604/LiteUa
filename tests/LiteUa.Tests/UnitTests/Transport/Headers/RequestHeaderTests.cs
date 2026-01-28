using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using Moq;

namespace LiteUa.Tests.UnitTests.Transport.Headers
{
    [Trait("Category", "Unit")]
    public class RequestHeaderTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public RequestHeaderTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_SetsStandardOpcUaDefaults()
        {
            // Act
            var header = new RequestHeader();

            // Assert
            Assert.Equal(0u, header.AuthenticationToken.NumericIdentifier);
            Assert.Equal(0u, header.RequestHandle);
            Assert.Equal(0u, header.ReturnDiagnostics);
            Assert.Equal(10000u, header.TimeoutHint);
            Assert.Null(header.AuditEntryId);
            Assert.NotNull(header.AdditionalHeader);
            Assert.True((DateTime.UtcNow - header.Timestamp).TotalSeconds < 5);
        }

        [Fact]
        public void Encode_WritesAllFieldsWithCorrectValues()
        {
            // Arrange
            var timestamp = new DateTime(2024, 5, 20, 10, 0, 0, DateTimeKind.Utc);
            var header = new RequestHeader
            {
                AuthenticationToken = new NodeId(1, 1234u),
                Timestamp = timestamp,
                RequestHandle = 55,
                ReturnDiagnostics = 1,
                AuditEntryId = "Audit-001",
                TimeoutHint = 5000,
                AdditionalHeader = new ExtensionObject { Encoding = 0x00 }
            };

            // Act
            header.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt32(1), Times.AtLeastOnce);
            _writerMock.Verify(w => w.WriteUInt16((UInt16)1234u), Times.Once);
            _writerMock.Verify(w => w.WriteDateTime(timestamp), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(55u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(1u), Times.Once);
            _writerMock.Verify(w => w.WriteString("Audit-001"), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(5000u), Times.Once);
        }

        [Fact]
        public void Encode_NullAdditionalHeader_EncodesExtensionObjectNull()
        {
            // Arrange
            var header = new RequestHeader { AdditionalHeader = null! };

            // Act
            header.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteByte(0x00), Times.AtLeast(3));
        }

        [Fact]
        public void Encode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var header = new RequestHeader
            {
                AuthenticationToken = new NodeId(0, 0u),
                RequestHandle = 1111,
                ReturnDiagnostics = 2222,
                TimeoutHint = 3333
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteUInt32(1111)).Callback(() => callOrder.Add("Handle"));
            _writerMock.Setup(w => w.WriteUInt32(2222)).Callback(() => callOrder.Add("Diagnostics"));
            _writerMock.Setup(w => w.WriteUInt32(3333)).Callback(() => callOrder.Add("Timeout"));

            // Act
            header.Encode(_writerMock.Object);

            // Assert
            // ... -> Timestamp -> Handle -> Diagnostics -> AuditEntry -> Timeout -> AdditionalHeader
            Assert.Equal("Handle", callOrder[0]);
            Assert.Equal("Diagnostics", callOrder[1]);
            Assert.Equal("Timeout", callOrder[2]);
        }

        [Fact]
        public void Encode_NullAuditEntryId_CallsWriterWithNull()
        {
            // Arrange
            var header = new RequestHeader { AuditEntryId = null };

            // Act
            header.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteString(null), Times.Once);
        }
    }
}