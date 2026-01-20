using LiteUa.Encoding;
using LiteUa.Stack.View;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class TranslateBrowsePathsToNodeIdsResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public TranslateBrowsePathsToNodeIdsResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(557u, TranslateBrowsePathsToNodeIdsResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_StandardResponse_SetsProperties()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.UtcNow);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header
                .Returns(1) // Response Results Count
                .Returns(0); // BrowsePathResult Targets Count
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u); // Good

            // Skip DiagnosticInfo block (Position == Length)
            _readerMock.Setup(r => r.Position).Returns(100);
            _readerMock.Setup(r => r.Length).Returns(100);

            // Act
            var response = new TranslateBrowsePathsToNodeIdsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ResponseHeader);
            Assert.NotNull(response.Results);
            Assert.Single(response.Results);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_WithDiagnostics_ParsesAllFields()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header
                .Returns(1) // Results Count
                .Returns(0) // Internal result count
                .Returns(1); // Diagnostics Count

            // Trigger DiagnosticInfo read
            _readerMock.Setup(r => r.Position).Returns(0);
            _readerMock.Setup(r => r.Length).Returns(500);

            // Act
            var response = new TranslateBrowsePathsToNodeIdsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.Results);
            Assert.Single(response.Results);
            Assert.NotNull(response.DiagnosticInfos);
            Assert.Single(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_NullResults_HandlesNegativeOne()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)  // Header
                .Returns(-1); // Results Count

            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new TranslateBrowsePathsToNodeIdsResponse();
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
            var response = new TranslateBrowsePathsToNodeIdsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("HeaderTime", callOrder[0]);
            Assert.Equal("ResultsCount", callOrder[1]);
        }
    }
}
