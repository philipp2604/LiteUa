using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class DeleteSubscriptionsResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public DeleteSubscriptionsResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(850u, DeleteSubscriptionsResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_StandardResponse_ParsesResults()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.UtcNow);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u); // Handle/Result
            _readerMock.Setup(r => r.ReadByte()).Returns(0);   // DiagMask/ExtraMask
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)     // - Header StringTable (0)
                .Returns(1);    // - Results Count (1)
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u) // Header Handle
                .Returns(0u) // Header Result
                .Returns(0u); // Result[0] (Good)

            // Skip DiagnosticInfo block (Position == Length)
            _readerMock.Setup(r => r.Position).Returns(100);
            _readerMock.Setup(r => r.Length).Returns(100);

            // Act
            var response = new DeleteSubscriptionsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ResponseHeader);
            Assert.NotNull(response.Results);
            Assert.Single(response.Results);
            Assert.True(response.Results[0].IsGood);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_WithDiagnostics_ParsesDiagnosticArray()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);

            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header
                .Returns(1) // Results Count
                .Returns(1); // Diagnostics Count

            // Trigger diag block
            _readerMock.Setup(r => r.Position).Returns(0);
            _readerMock.Setup(r => r.Length).Returns(500);

            // Act
            var response = new DeleteSubscriptionsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.Results);
            Assert.Single(response.Results);
            Assert.NotNull(response.DiagnosticInfos);
            Assert.Single(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_NullOrEmptyResults_HandlesGracefully()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Count = 0 (Empty) or -1 (Null)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)  // Header
                .Returns(0); // Results Count

            // Skip Diags
            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new DeleteSubscriptionsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Null(response.Results);
        }

        [Fact]
        public void Decode_SequenceCheck_VerifiesFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadDateTime()).Returns(() => {
                callOrder.Add("HeaderTime");
                return DateTime.MinValue;
            });

            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header StringTable
                .Returns(() => {
                    callOrder.Add("ResultsCount");
                    return 0;
                });

            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new DeleteSubscriptionsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("HeaderTime", callOrder[0]);
            Assert.Equal("ResultsCount", callOrder[1]);
        }
    }
}
