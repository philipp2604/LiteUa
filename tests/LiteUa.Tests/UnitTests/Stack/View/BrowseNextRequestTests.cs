using LiteUa.Encoding;
using LiteUa.Stack.View;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class BrowseNextRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public BrowseNextRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(533u, BrowseNextRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_InitializesDefaults()
        {
            // Act
            var request = new BrowseNextRequest();

            // Assert
            Assert.NotNull(request.RequestHeader);
            Assert.False(request.ReleaseContinuationPoints);
            Assert.Null(request.ContinuationPoints);
        }

        [Fact]
        public void Encode_NullContinuationPoints_WritesNegativeOne()
        {
            // Arrange
            var request = new BrowseNextRequest
            {
                ReleaseContinuationPoints = true,
                ContinuationPoints = null
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt16(533), Times.Once);
            _writerMock.Verify(w => w.WriteBoolean(true), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithPoints_WritesCountAndByteStrings()
        {
            // Arrange
            byte[][] points =
            [
            [0xAA, 0xBB],
            [0xCC, 0xDD]
        ];
            var request = new BrowseNextRequest
            {
                ContinuationPoints = points
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once);
            _writerMock.Verify(w => w.WriteByteString(points[0]), Times.Once);
            _writerMock.Verify(w => w.WriteByteString(points[1]), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new BrowseNextRequest
            {
                ReleaseContinuationPoints = true,
                ContinuationPoints = [[1]]
            };

            var callOrder = new List<string>();

            // NodeId (TypeID) -> Boolean -> Array length
            _writerMock.Setup(w => w.WriteUInt16(533))
                       .Callback(() => callOrder.Add("TypeID"));

            _writerMock.Setup(w => w.WriteBoolean(true))
                       .Callback(() => callOrder.Add("ReleaseFlag"));

            _writerMock.Setup(w => w.WriteInt32(1))
                       .Callback(() => callOrder.Add("ArrayLength"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int typeIdx = callOrder.IndexOf("TypeID");
            int boolIdx = callOrder.IndexOf("ReleaseFlag");
            int lengthIdx = callOrder.IndexOf("ArrayLength");

            Assert.True(typeIdx < boolIdx);
            Assert.True(boolIdx < lengthIdx);
        }
    }
}
