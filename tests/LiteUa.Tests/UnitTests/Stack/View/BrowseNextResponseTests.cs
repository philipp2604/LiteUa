using LiteUa.Encoding;
using LiteUa.Stack.View;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class BrowseNextResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public BrowseNextResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            // BrowseNextResponse identifier is 536
            Assert.Equal(536u, BrowseNextResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_BasicResponse_ParsesResults()
        {
            // Arrange
            // 1. ResponseHeader sequence
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.UtcNow);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u); // Handle/Result
            _readerMock.Setup(r => r.ReadByte()).Returns(0);   // Masks

            // 2. ReadInt32 Sequence:
            // - Header StringTable (0)
            // - Results Count (1)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(1);

            // 3. BrowseResult.Decode internal requirements:
            // (Assuming typical: StatusCode, ContinuationPoint/ByteString, ReferenceDescription Array Count)
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)    // Header Handle
                .Returns(0u)    // Header Result
                .Returns(0u);   // Result StatusCode (Good)

            _readerMock.Setup(r => r.ReadByteString()).Returns((byte[]?)null); // No ContinuationPoint

            // 4. Ensure we don't enter DiagnosticInfo block (Position == Length)
            _readerMock.Setup(r => r.Position).Returns(100);
            _readerMock.Setup(r => r.Length).Returns(100);

            // Act
            var response = new BrowseNextResponse();
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
            _readerMock.Setup(r => r.ReadBoolean()).Returns(true);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header
                .Returns(1) // Results Count
                .Returns(1) // References Count
                .Returns(1); // Diagnostics Count
            _readerMock.Setup(r => r.ReadString()).Returns("ValidBrowseName");
            _readerMock.Setup(r => r.ReadByteString()).Returns([]); // ContinuationPoint
            _readerMock.Setup(r => r.ReadUInt16()).Returns(0); // QualifiedName Namespace

            // trigger the diagnostics block
            _readerMock.Setup(r => r.Position).Returns(0);
            _readerMock.Setup(r => r.Length).Returns(500);

            // Act
            var response = new BrowseNextResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.Results);
            Assert.Single(response.Results);
            Assert.NotNull(response.Results[0].References);
            Assert.Single(response.Results[0].References!);
            Assert.Equal("ValidBrowseName", (string)response.Results[0].References![0].BrowseName!.Name);
            Assert.NotNull(response.DiagnosticInfos);
            Assert.Single(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_NullOrEmptyResults_HandlesGracefully()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Header StringTable (0), then Results Count (-1)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(-1);

            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            var response = new BrowseNextResponse();
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
            var response = new BrowseNextResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("HeaderTime", callOrder[0]);
            Assert.Equal("ResultsCount", callOrder[1]);
        }
    }
}
