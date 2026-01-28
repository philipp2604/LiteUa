using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Attribute
{
    [Trait("Category", "Unit")]
    public class ReadValueIdTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public ReadValueIdTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange
            var nodeId = new NodeId(100);

            // Act
            var rvid = new ReadValueId(nodeId);

            // Assert
            Assert.Equal(nodeId, rvid.NodeId);
            Assert.Equal(13u, rvid.AttributeId); // Default: Value
            Assert.Null(rvid.IndexRange);
            Assert.Null(rvid.DataEncoding);
        }

        [Fact]
        public void Encode_DefaultValues_WritesCorrectSequence()
        {
            // Arrange
            var rvid = new ReadValueId(new NodeId(200));
            // Defaults: AttributeId=13, IndexRange=null, DataEncoding=null

            // Act
            rvid.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteByte(0x00), Times.Once);
            _writerMock.Verify(w => w.WriteByte(200), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(13u), Times.Once);
            _writerMock.Verify(w => w.WriteString(null), Times.AtLeastOnce);
            _writerMock.Verify(w => w.WriteUInt16(0), Times.Once);
            _writerMock.Verify(w => w.WriteString(null), Times.Exactly(2));
        }

        [Fact]
        public void Encode_CustomValues_WritesCorrectSequence()
        {
            // Arrange
            var rvid = new ReadValueId(new NodeId(1, "MyNode"))
            {
                AttributeId = 10, // BrowseName
                IndexRange = "1:5",
                DataEncoding = new QualifiedName(2, "CustomEncoding")
            };

            // Act
            rvid.Encode(_writerMock.Object);

            // Assert
            // Verify AttributeId
            _writerMock.Verify(w => w.WriteUInt32(10u), Times.Once);

            // Verify IndexRange String
            _writerMock.Verify(w => w.WriteString("1:5"), Times.Once);

            // Verify DataEncoding QualifiedName
            _writerMock.Verify(w => w.WriteUInt16(2), Times.Once);
            _writerMock.Verify(w => w.WriteString("CustomEncoding"), Times.Once);
        }

        [Fact]
        public void Encode_VerifyOrder()
        {
            // Arrange
            var rvid = new ReadValueId(new NodeId(0))
            {
                AttributeId = 99u,
                IndexRange = "test"
            };

            var callOrder = new System.Collections.Generic.List<string>();

            _writerMock.Setup(w => w.WriteUInt32(99u)).Callback(() => callOrder.Add("Attr"));
            _writerMock.Setup(w => w.WriteString("test")).Callback(() => callOrder.Add("Range"));
            _writerMock.Setup(w => w.WriteUInt16(0)).Callback(() => callOrder.Add("EncodingNS"));

            // Act
            rvid.Encode(_writerMock.Object);

            // Assert
            Assert.Equal("Attr", callOrder[0]);
            Assert.Equal("Range", callOrder[1]);
            Assert.Equal("EncodingNS", callOrder[2]);
        }
    }
}