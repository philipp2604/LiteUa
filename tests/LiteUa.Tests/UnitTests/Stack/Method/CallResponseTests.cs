using LiteUa.Encoding;
using LiteUa.Stack.Method;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Method
{
    [Trait("Category", "Unit")]
    public class CallResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public CallResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(715u, CallResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_BasicResponse_ParsesCorrectly()
        {
            // Arrange
            // 1. Header noise (Int64, UInt32, UInt32, Byte, Byte)
            _readerMock.Setup(r => r.ReadInt64()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // 2. Int32 Sequence:
            // - Header StringTable (0)
            // - CallResponse Results Count (1)
            // - CallMethodResponse: Results Count (0)
            // - CallMethodResponse: Diags Count (0)
            // - CallMethodResponse: OutputArgs Count (0)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header
                .Returns(1) // CallResponse.Results count
                .Returns(0) // Internal CallMethodResponse array 1
                .Returns(0) // Internal CallMethodResponse array 2
                .Returns(0); // Internal CallMethodResponse array 3

            // 3. Prevent optional DiagnosticInfo block
            _readerMock.Setup(r => r.Position).Returns(100);
            _readerMock.Setup(r => r.Length).Returns(100);

            // Act
            var response = new CallResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ResponseHeader);
            Assert.NotNull(response.Results);
            Assert.Single(response.Results);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_WithDiagnostics_ParsesBothArrays()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadInt64()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // 1. Results Count = 1, then Diag Count = 1
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header
                .Returns(1) // CallResponse.Results count
                .Returns(0) // Internal CallMethodResponse array 1
                .Returns(0) // Internal CallMethodResponse array 2
                .Returns(0) // Internal CallMethodResponse array 3
                .Returns(1); // CallResponse.DiagnosticInfos count
            _readerMock.Setup(r => r.Position).Returns(0);
            _readerMock.Setup(r => r.Length).Returns(500);

            // Act
            var response = new CallResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.Results);
            Assert.Single(response.Results);
            Assert.NotNull(response.DiagnosticInfos);
            Assert.Single(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_EmptyResults_HandlesCountZero()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Header StringTable (0), then Results Count (0)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(0);

            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new CallResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Null(response.Results);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_NullResults_HandlesNegativeOne()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Header StringTable (0), then Results Count (-1)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(-1);

            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new CallResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Null(response.Results);
        }
    }
}
