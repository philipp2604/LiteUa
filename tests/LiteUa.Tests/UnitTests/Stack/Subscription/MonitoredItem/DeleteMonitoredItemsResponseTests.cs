using LiteUa.Encoding;
using LiteUa.Stack.Subscription.MonitoredItem;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription.MonitoredItem
{
    [Trait("Category", "Unit")]
    public class DeleteMonitoredItemsResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public DeleteMonitoredItemsResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(784u, DeleteMonitoredItemsResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_BasicResponse_ParsesResults()
        {
            // Arrange
            // 1. ResponseHeader sequence
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.UtcNow);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(2);
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)    // Header Handle
                .Returns(0u)    // Header Result
                .Returns(0u)    // Result 1: Good
                .Returns(0x80010000u); // Result 2: Bad_UnexpectedError

            // Skip DiagnosticInfo block
            _readerMock.Setup(r => r.Position).Returns(100);
            _readerMock.Setup(r => r.Length).Returns(100);

            // Act
            var response = new DeleteMonitoredItemsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ResponseHeader);
            Assert.NotNull(response.Results);
            Assert.Equal(2, response.Results.Length);
            Assert.True(response.Results[0].IsGood);
            Assert.True(response.Results[1].IsBad);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_WithDiagnostics_ParsesAllFields()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0);

            // 1. Header StringTable (0)
            // 2. Results Count (1)
            // 3. Diagnostics Count (1)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header
                .Returns(1) // Results
                .Returns(1); // Diagnostics

            // Trigger optional diags
            _readerMock.Setup(r => r.Position).Returns(0);
            _readerMock.Setup(r => r.Length).Returns(500);

            // Act
            var response = new DeleteMonitoredItemsResponse();
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
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Header StringTable (0), Results Count (0)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(0);

            // Skips diagnostics
            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new DeleteMonitoredItemsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Null(response.Results);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_NullResults_HandlesNegativeOne()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Results Count (-1)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(-1);

            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new DeleteMonitoredItemsResponse();
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

            // Act
            var response = new DeleteMonitoredItemsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("HeaderTime", callOrder[0]);
            Assert.Equal("ResultsCount", callOrder[1]);
        }
    }
}
