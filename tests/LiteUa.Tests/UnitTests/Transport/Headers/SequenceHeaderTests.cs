using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using Moq;

namespace LiteUa.Tests.UnitTests.Transport.Headers
{
    [Trait("Category", "Unit")]
    public class SequenceHeaderTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public SequenceHeaderTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_InitializesWithDefaultValues()
        {
            // Act
            var header = new SequenceHeader();

            // Assert
            Assert.Equal(0u, header.SequenceNumber);
            Assert.Equal(0u, header.RequestId);
        }

        [Fact]
        public void Encode_WritesCorrectValuesInSequence()
        {
            // Arrange
            var header = new SequenceHeader
            {
                SequenceNumber = 1001,
                RequestId = 55
            };

            // Act
            header.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt32(1001u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(55u), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var header = new SequenceHeader
            {
                SequenceNumber = 7777,
                RequestId = 8888
            };

            var callOrder = new List<uint>();
            _writerMock.Setup(w => w.WriteUInt32(7777))
                       .Callback(() => callOrder.Add(7777));

            _writerMock.Setup(w => w.WriteUInt32(8888))
                       .Callback(() => callOrder.Add(8888));

            // Act
            header.Encode(_writerMock.Object);

            // Assert
            Assert.Equal(2, callOrder.Count);
            Assert.Equal(7777u, callOrder[0]); // SequenceNumber
            Assert.Equal(8888u, callOrder[1]); // RequestId
        }

        [Fact]
        public void Properties_AreMutable()
        {
            // Arrange
            var header = new SequenceHeader
            {
                // Act
                SequenceNumber = 10,
                RequestId = 20
            };

            // Assert
            Assert.Equal(10u, header.SequenceNumber);
            Assert.Equal(20u, header.RequestId);
        }
    }
}