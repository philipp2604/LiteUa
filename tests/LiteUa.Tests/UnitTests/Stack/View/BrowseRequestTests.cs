using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.View;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class BrowseRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public BrowseRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(527u, BrowseRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var request = new BrowseRequest();

            // Assert
            Assert.Equal(0u, request.RequestedMaxReferencesPerNode);
            Assert.NotNull(request.RequestHeader);
            Assert.Null(request.NodesToBrowse);
        }

        [Fact]
        public void Encode_NullNodesToBrowse_WritesNegativeOne()
        {
            // Arrange
            var request = new BrowseRequest
            {
                RequestedMaxReferencesPerNode = 100,
                NodesToBrowse = null
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt32(100u), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_FullRequest_WritesCorrectSequence()
        {
            // Arrange
            var node1 = new BrowseDescription(new NodeId(101));
            var request = new BrowseRequest
            {
                RequestedMaxReferencesPerNode = 50,
                NodesToBrowse = [node1]
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt16(527), Times.Once);
            _writerMock.Verify(w => w.WriteByte(0x00), Times.AtLeast(2));
            _writerMock.Verify(w => w.WriteDateTime(DateTime.MinValue), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(0u), Times.AtLeastOnce);
            _writerMock.Verify(w => w.WriteUInt32(50u), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(1), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new BrowseRequest
            {
                RequestedMaxReferencesPerNode = 9999,
                NodesToBrowse = [new BrowseDescription(new NodeId(0))]
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteDateTime(DateTime.MinValue))
                       .Callback(() => callOrder.Add("ViewTimestamp"));

            _writerMock.Setup(w => w.WriteUInt32(9999))
                       .Callback(() => callOrder.Add("MaxRefs"));

            _writerMock.Setup(w => w.WriteInt32(1))
                       .Callback(() => callOrder.Add("ArrayLength"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // Header -> ViewDescription -> RequestedMaxReferencesPerNode -> NodesToBrowse
            int viewIdx = callOrder.IndexOf("ViewTimestamp");
            int maxIdx = callOrder.IndexOf("MaxRefs");
            int arrayIdx = callOrder.IndexOf("ArrayLength");

            Assert.True(viewIdx < maxIdx);
            Assert.True(maxIdx < arrayIdx);
        }
    }
}
