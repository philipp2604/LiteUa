using LiteUa.Encoding;
using LiteUa.Stack.Subscription.MonitoredItem;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription.MonitoredItem
{
    [Trait("Category", "Unit")]
    public class CreateMonitoredItemsResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public CreateMonitoredItemsResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(754u, CreateMonitoredItemsResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_BasicResponse_ParsesResults()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.UtcNow);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(1);

            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)    // Header Handle
                .Returns(0u)    // Header Result
                .Returns(0u)    // Item StatusCode (Good)
                .Returns(101u)  // MonitoredItemId
                .Returns(1u);   // RevisedQueueSize
            _readerMock.Setup(r => r.ReadDouble()).Returns(1000.0); // RevisedSampling

            // Skip DiagnosticInfo block
            _readerMock.Setup(r => r.Position).Returns(100);
            _readerMock.Setup(r => r.Length).Returns(100);

            // Act
            var response = new CreateMonitoredItemsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ResponseHeader);
            Assert.NotNull(response.Results);
            Assert.Single(response.Results);
            Assert.Equal(101u, response.Results[0].MonitoredItemId);
            Assert.Equal(1000.0, response.Results[0].RevisedSamplingInterval);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_WithDiagnostics_ParsesAllFields()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header
                .Returns(1) // Results
                .Returns(1); // Diagnostics

            // Don't skip diagnostics
            _readerMock.Setup(r => r.Position).Returns(0);
            _readerMock.Setup(r => r.Length).Returns(500);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);

            // Act
            var response = new CreateMonitoredItemsResponse();
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
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(0);

            // skips diagnostics
            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new CreateMonitoredItemsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Null(response.Results);
            Assert.Null(response.DiagnosticInfos);
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
            var response = new CreateMonitoredItemsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("HeaderTime", callOrder[0]);
            Assert.Equal("ResultsCount", callOrder[1]);
        }
    }
}
