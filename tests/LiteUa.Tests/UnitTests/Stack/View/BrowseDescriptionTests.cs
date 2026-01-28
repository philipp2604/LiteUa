using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.View;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class BrowseDescriptionTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public BrowseDescriptionTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_SetsPropertiesAndDefaults()
        {
            // Arrange
            var nodeId = new NodeId(1001);

            // Act
            var desc = new BrowseDescription(nodeId);

            // Assert
            Assert.Equal(nodeId, desc.NodeId);
            Assert.Equal(BrowseDirection.Forward, desc.BrowseDirection);
            Assert.Equal(33u, desc.ReferenceTypeId.NumericIdentifier); // HierarchicalReferences
            Assert.True(desc.IncludeSubtypes);
            Assert.Equal(0u, desc.NodeClassMask);
            Assert.Equal(63u, desc.ResultMask);
        }

        [Fact]
        public void Constructor_NullNodeId_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BrowseDescription(null!));
        }

        [Fact]
        public void Encode_WritesCorrectValuesInSequence()
        {
            // Arrange
            var nodeId = new NodeId(0, 50u);
            var refType = new NodeId(0, 40u);
            var desc = new BrowseDescription(nodeId)
            {
                BrowseDirection = BrowseDirection.Inverse,
                ReferenceTypeId = refType,
                IncludeSubtypes = false,
                NodeClassMask = 16, // Variables
                ResultMask = 1      // ReferenceType only
            };

            // Act
            desc.Encode(_writerMock.Object);

            // Assert
            // 1. Target NodeId (Numeric 50)
            _writerMock.Verify(w => w.WriteByte(50), Times.Once);
            // 2. BrowseDirection (Int32)
            _writerMock.Verify(w => w.WriteInt32((int)BrowseDirection.Inverse), Times.Once);
            // 3. ReferenceTypeId (Numeric 40)
            _writerMock.Verify(w => w.WriteByte(40), Times.Once);
            // 4. IncludeSubtypes (Boolean)
            _writerMock.Verify(w => w.WriteBoolean(false), Times.Once);
            // 5. NodeClassMask
            _writerMock.Verify(w => w.WriteUInt32(16u), Times.Once);
            // 6. ResultMask
            _writerMock.Verify(w => w.WriteUInt32(1u), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var desc = new BrowseDescription(new NodeId(0))
            {
                BrowseDirection = (BrowseDirection)111,
                NodeClassMask = 222u,
                ResultMask = 333u
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteInt32(111)).Callback(() => callOrder.Add("Direction"));
            _writerMock.Setup(w => w.WriteUInt32(222u)).Callback(() => callOrder.Add("ClassMask"));
            _writerMock.Setup(w => w.WriteUInt32(333u)).Callback(() => callOrder.Add("ResultMask"));

            // Act
            desc.Encode(_writerMock.Object);

            // Assert
            // NodeId -> Direction -> RefTypeId -> IncludeSubtypes -> ClassMask -> ResultMask
            Assert.Equal("Direction", callOrder[0]);
            Assert.Equal("ClassMask", callOrder[1]);
            Assert.Equal("ResultMask", callOrder[2]);
        }
    }
}