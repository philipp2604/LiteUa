using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.View;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class TranslateBrowsePathsToNodeIdsRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public TranslateBrowsePathsToNodeIdsRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(554u, TranslateBrowsePathsToNodeIdsRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_InitializesDefaults()
        {
            // Act
            var request = new TranslateBrowsePathsToNodeIdsRequest();

            // Assert
            Assert.NotNull(request.RequestHeader);
            Assert.Null(request.BrowsePaths);
        }

        [Fact]
        public void Encode_NullBrowsePaths_WritesNegativeOne()
        {
            // Arrange
            var request = new TranslateBrowsePathsToNodeIdsRequest { BrowsePaths = null };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // NodeId (554 > 255 uses FourByte 0x01 or Numeric 0x02)
            _writerMock.Verify(w => w.WriteUInt16(554), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithPaths_WritesCountAndDelegates()
        {
            // Arrange
            var path1 = new BrowsePath(new NodeId(101), new RelativePath([]));
            var path2 = new BrowsePath(new NodeId(102), new RelativePath([]));

            var request = new TranslateBrowsePathsToNodeIdsRequest
            {
                BrowsePaths = [path1, path2]
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once);
            _writerMock.Verify(w => w.WriteByte(101), Times.Once);
            _writerMock.Verify(w => w.WriteByte(102), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new TranslateBrowsePathsToNodeIdsRequest
            {
                BrowsePaths = [new BrowsePath(new NodeId(0), new RelativePath([]))]
            };

            var callOrder = new List<string>();

            // ServiceNodeId -> Header (Timestamp/Handle) -> Array
            _writerMock.Setup(w => w.WriteUInt16(554))
                       .Callback(() => callOrder.Add("ServiceID"));

            _writerMock.Setup(w => w.WriteInt32(1))
                       .Callback(() => callOrder.Add("ArrayLength"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int serviceIdx = callOrder.IndexOf("ServiceID");
            int lengthIdx = callOrder.IndexOf("ArrayLength");

            Assert.True(serviceIdx != -1);
            Assert.True(lengthIdx != -1);
            Assert.True(serviceIdx < lengthIdx);
        }
    }
}