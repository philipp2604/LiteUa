using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class SubscriptionAcknowledgementTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public SubscriptionAcknowledgementTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_InitializesWithDefaultValues()
        {
            // Act
            var ack = new SubscriptionAcknowledgement();

            // Assert
            Assert.Equal(0u, ack.SubscriptionId);
            Assert.Equal(0u, ack.SequenceNumber);
        }

        [Fact]
        public void Encode_WritesCorrectValuesInSequence()
        {
            // Arrange
            var ack = new SubscriptionAcknowledgement
            {
                SubscriptionId = 12345,
                SequenceNumber = 99
            };

            // Act
            ack.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt32(12345u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(99u), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var ack = new SubscriptionAcknowledgement
            {
                SubscriptionId = 7777,
                SequenceNumber = 8888
            };

            var callOrder = new List<uint>();
            _writerMock.Setup(w => w.WriteUInt32(7777))
                       .Callback(() => callOrder.Add(7777));
            _writerMock.Setup(w => w.WriteUInt32(8888))
                       .Callback(() => callOrder.Add(8888));

            // Act
            ack.Encode(_writerMock.Object);

            // Assert
            Assert.Equal(2, callOrder.Count);
            Assert.Equal(7777u, callOrder[0]);
            Assert.Equal(8888u, callOrder[1]);
        }

        [Fact]
        public void Properties_AreMutable()
        {
            // Arrange
            var ack = new SubscriptionAcknowledgement
            {
                // Act
                SubscriptionId = 10,
                SequenceNumber = 20
            };

            // Assert
            Assert.Equal(10u, ack.SubscriptionId);
            Assert.Equal(20u, ack.SequenceNumber);
        }
    }
}