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
    public class BrowsePathTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public BrowsePathTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var startNode = new NodeId(2258);
            var relPath = new RelativePath([new RelativePathElement()]);

            // Act
            var browsePath = new BrowsePath(startNode, relPath);

            // Assert
            Assert.Same(startNode, browsePath.StartingNode);
            Assert.Same(relPath, browsePath.RelativePath);
        }

        [Fact]
        public void Encode_WritesCorrectValuesInSequence()
        {
            // Arrange
            var startNode = new NodeId(101u);
            var relPath = new RelativePath([new RelativePathElement()]);

            var browsePath = new BrowsePath(startNode, relPath);

            // Act
            browsePath.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteByte(0), Times.Exactly(2));
            _writerMock.Verify(w => w.WriteByte(101), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(1), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var startNode = new NodeId(0, 55u);
            var relPath = new RelativePath([new RelativePathElement()]);
            var browsePath = new BrowsePath(startNode, relPath);

            var callOrder = new List<string>();

            // NodeId byte -> RelativePath array length
            _writerMock.Setup(w => w.WriteByte(55))
                       .Callback(() => callOrder.Add("StartingNode"));

            _writerMock.Setup(w => w.WriteByte(33))
                       .Callback(() => callOrder.Add("RelativePath"));

            // Act
            browsePath.Encode(_writerMock.Object);

            // Assert
            Assert.Equal("StartingNode", callOrder[0]);
            Assert.Equal("RelativePath", callOrder[1]);
        }

        [Fact]
        public void Properties_AreMutable()
        {
            // Arrange
            var browsePath = new BrowsePath(new NodeId(0), new RelativePath([new RelativePathElement()]));
            var newNode = new NodeId(999);
            var newPath = new RelativePath([new RelativePathElement()]);

            // Act
            browsePath.StartingNode = newNode;
            browsePath.RelativePath = newPath;

            // Assert
            Assert.Same(newNode, browsePath.StartingNode);
            Assert.Same(newPath, browsePath.RelativePath);
        }
    }
}
