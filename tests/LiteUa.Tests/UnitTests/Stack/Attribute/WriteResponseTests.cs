using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Attribute
{
    [Trait("Category", "Unit")]
    public class WriteResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public WriteResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            // OPC UA WriteResponse identifier is 676
            Assert.Equal(676u, WriteResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_BasicResponse_ParsesResults()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Results Count = 2
            _readerMock.Setup(r => r.ReadInt32()).Returns(2);

            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0x00000000u)
                .Returns(0x00000000u)
                .Returns(0x00000000u) // Good
                .Returns(0x800A0000u); // Bad_Timeout

            // 4. Don't enter DiagnosticInfo block
            _readerMock.Setup(r => r.Position).Returns(100);
            _readerMock.Setup(r => r.Length).Returns(100);

            // Act
            var response = new WriteResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.Results);
            Assert.Equal(2, response.Results.Length);
            Assert.True(response.Results[0].IsGood);
            Assert.True(response.Results[1].IsBad);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_WithDiagnostics_ParsesBothArrays()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Sequence for ReadInt32: 
            // 1. Header
            // 2. Results Count (1)
            // 3. Diagnostic Count (1)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(1)
                .Returns(1);

            // Position < Length to trigger the Diag block
            _readerMock.Setup(r => r.Position).Returns(0);
            _readerMock.Setup(r => r.Length).Returns(50);

            // Act
            var response = new WriteResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Single(response.Results!);
            Assert.Single(response.DiagnosticInfos!);
        }

        [Fact]
        public void Decode_EmptyResults_HandlesCountZero()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0); // Header
            _readerMock.Setup(r => r.ReadInt32()).Returns(0); // Count 0

            // Simulation of end of stream
            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new WriteResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Null(response.Results);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_PartialStream_SkipsOptionalDiagnostics()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0); // Header
            _readerMock.Setup(r => r.ReadInt32()).Returns(1); // 1 Result

            // Force Position == Length (No data left for diags)
            _readerMock.Setup(r => r.Position).Returns(20);
            _readerMock.Setup(r => r.Length).Returns(20);

            // Act
            var response = new WriteResponse();
            response.Decode(_readerMock.Object);

            // Assert
            // Verify only the Result Int32 was read (after the header)
            _readerMock.Verify(r => r.ReadInt32(), Times.Exactly(2));
            Assert.Null(response.DiagnosticInfos);
        }
    }
}
