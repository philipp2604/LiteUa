using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Attribute
{
    [Trait("Category", "Unit")]
    public class ReadResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public ReadResponseTests()
        {
            // Mocking the reader with a dummy stream
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(634u, ReadResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_BasicResponse_SetsProperties()
        {
            // Arrange
            // 1. ResponseHeader.Decode will consume data. 
            _readerMock.SetupSequence(r => r.ReadByte()).Returns(0); // NodeId Type 0

            // 2. Results Count = 1
            _readerMock.SetupSequence(r => r.ReadInt32()).Returns(0).Returns(1);

            // 3. DataValue.Decode will consume data (Mask byte)
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0) // NodeId (Header)
                .Returns(0) // Mask (DataValue)
                .Returns(0); // To terminate any potential subsequent reads

            // Act
            var response = new ReadResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ResponseHeader);
            Assert.Single(response.Results!);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_MultipleResultsAndDiagnostics_ParsesCorrectly()
        {
            // Arrange
            // Setup Reader Position/Length to allow DiagnosticInfo block
            _readerMock.Setup(r => r.Position).Returns(0);
            _readerMock.Setup(r => r.Length).Returns(100);

            // Int32 Sequence: 
            // 1. Header (string table) = 0
            // 2. Results Count = 2
            // 3. Diag Count = 1
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // StringTable entries
                .Returns(2) // Results
                .Returns(1); // Diagnostics

            // Provide enough bytes for the static decoders
            // Header (0), DataValue1 Mask (0), DataValue2 Mask (0), Diag Mask (0)
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0) // Header
                .Returns(0) // DV1
                .Returns(0) // DV2
                .Returns(0); // Diag Mask (0 = null)

            // Act
            var response = new ReadResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(2, response.Results!.Length);
            Assert.Single(response.DiagnosticInfos!);
        }

        [Fact]
        public void Decode_EmptyResults_HandlesCountZero()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0); // Header
            _readerMock.Setup(r => r.ReadInt32()).Returns(0); // Count 0

            // Simulate end of stream for Diags check
            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new ReadResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Null(response.Results);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_EndOfStream_SkipsDiagnosticInfos()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0); // Header
            _readerMock.Setup(r => r.ReadInt32()).Returns(1); // 1 Result

            // Setup end of stream condition
            _readerMock.Setup(r => r.Position).Returns(50);
            _readerMock.Setup(r => r.Length).Returns(50);

            // Act
            var response = new ReadResponse();
            response.Decode(_readerMock.Object);

            // Assert
            _readerMock.Verify(r => r.ReadInt32(), Times.Exactly(2)); // Header + Results count
            Assert.Null(response.DiagnosticInfos);
        }
    }
}
