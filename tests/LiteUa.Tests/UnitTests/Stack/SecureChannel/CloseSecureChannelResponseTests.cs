using LiteUa.Encoding;
using LiteUa.Stack.SecureChannel;
using LiteUa.Transport.Headers;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.SecureChannel
{
    [Trait("Category", "Unit")]
    public class CloseSecureChannelResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public CloseSecureChannelResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(455u, CloseSecureChannelResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_ValidStream_SetsResponseHeader()
        {
            // Arrange
            // ResponseHeader.Decode reads:
            // - Timestamp (DateTime)
            // - RequestHandle (UInt32)
            // - ServiceResult (UInt32)
            // - ServiceDiagnostics (Byte mask)
            // - StringTable (Int32 count)
            // - AdditionalHeader (Byte mask)

            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.Now);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);

            // Act
            var response = new CloseSecureChannelResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ResponseHeader);
            _readerMock.Verify(r => r.ReadDateTime(), Times.Once);
        }

        [Fact]
        public void ResponseHeader_Property_IsAssignable()
        {
            // Arrange
            var response = new CloseSecureChannelResponse();
            var header = new ResponseHeader();

            // Act
            response.ResponseHeader = header;

            // Assert
            Assert.Same(header, response.ResponseHeader);
        }
    }
}
