using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.View;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class RelativePathTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public RelativePathTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_SetsElementsProperty()
        {
            // Arrange
            var elements = new[] { new RelativePathElement() };

            // Act
            var relPath = new RelativePath(elements);

            // Assert
            Assert.Same(elements, relPath.Elements);
        }

        [Fact]
        public void Encode_NullElements_WritesNegativeOne()
        {
            // Arrange
            var relPath = new RelativePath(null!);

            // Act
            relPath.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_EmptyElements_WritesZeroLength()
        {
            // Arrange
            var relPath = new RelativePath([]);

            // Act
            relPath.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteInt32(0), Times.Once);
        }

        [Fact]
        public void Encode_PopulatedElements_WritesLengthAndDelegates()
        {
            // Arrange
            var elements = new[]
            {
            new RelativePathElement { TargetName = new QualifiedName(0, "A") },
            new RelativePathElement { TargetName = new QualifiedName(0, "B") }
        };
            var relPath = new RelativePath(elements);

            // Act
            relPath.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once);
            _writerMock.Verify(w => w.WriteString("A"), Times.Once);
            _writerMock.Verify(w => w.WriteString("B"), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesSequence_LengthBeforeElements()
        {
            // Arrange
            var elements = new[] { new RelativePathElement { TargetName = new QualifiedName(0, "Target") } };
            var relPath = new RelativePath(elements);
            var callOrder = new List<string>();

            _writerMock.Setup(w => w.WriteInt32(1)).Callback(() => callOrder.Add("Length"));
            _writerMock.Setup(w => w.WriteString("Target")).Callback(() => callOrder.Add("ElementContent"));

            // Act
            relPath.Encode(_writerMock.Object);

            // Assert
            Assert.Equal("Length", callOrder[0]);
            Assert.Equal("ElementContent", callOrder[1]);
        }
    }
}