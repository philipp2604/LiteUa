using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.View;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class BrowsePathTargetTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public BrowsePathTargetTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidStream_SetsProperties()
        {
            // Arrange
            uint expectedIndex = 0xFFFFFFFF; // Sentinel value for "no remaining index"
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0x00)  // Encoding Mask
                .Returns(22);   // NodeId Identifier (e.g., Node 22)
            _readerMock.Setup(r => r.ReadUInt32()).Returns(expectedIndex);

            // Act
            var result = BrowsePathTarget.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result.TargetId);
            Assert.Equal(22u, result.TargetId.NodeId.NumericIdentifier);
            Assert.Equal(expectedIndex, result.RemainingPathIndex);
        }

        [Fact]
        public void Decode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();
            _readerMock.Setup(r => r.ReadByte()).Returns(() =>
            {
                callOrder.Add("ExpandedNodeIdPart");
                return 0; // TwoByte NodeId
            });
            _readerMock.Setup(r => r.ReadUInt32()).Returns(() =>
            {
                callOrder.Add("Index");
                return 0u;
            });

            // Act
            BrowsePathTarget.Decode(_readerMock.Object);

            // Assert
            int firstIdPartIdx = callOrder.IndexOf("ExpandedNodeIdPart");
            int indexIdx = callOrder.IndexOf("Index");

            Assert.True(firstIdPartIdx != -1);
            Assert.True(indexIdx != -1);
            Assert.True(firstIdPartIdx < indexIdx);
        }

        [Fact]
        public void Properties_AreMutable()
        {
            // Arrange
            var target = new BrowsePathTarget();
            var id = new ExpandedNodeId { NodeId = new NodeId(100) };

            // Act
            target.TargetId = id;
            target.RemainingPathIndex = 5;

            // Assert
            Assert.Same(id, target.TargetId);
            Assert.Equal(5u, target.RemainingPathIndex);
        }
    }
}