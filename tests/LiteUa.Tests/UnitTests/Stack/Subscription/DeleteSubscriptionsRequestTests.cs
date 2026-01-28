using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class DeleteSubscriptionsRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public DeleteSubscriptionsRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(847u, DeleteSubscriptionsRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_InitializesDefaults()
        {
            // Act
            var request = new DeleteSubscriptionsRequest();

            // Assert
            Assert.NotNull(request.RequestHeader);
            Assert.Null(request.SubscriptionIds);
        }

        [Fact]
        public void Encode_NullSubscriptionIds_WritesNegativeOne()
        {
            // Arrange
            var request = new DeleteSubscriptionsRequest { SubscriptionIds = null };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // NodeId (847 > 255 uses FourByte 0x01 or Numeric 0x02)
            _writerMock.Verify(w => w.WriteUInt16(847), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithIds_WritesCountAndElements()
        {
            // Arrange
            uint[] ids = [500, 600, 700];
            var request = new DeleteSubscriptionsRequest { SubscriptionIds = ids };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. Array Length (3)
            _writerMock.Verify(w => w.WriteInt32(3), Times.Once);

            // 2. Individual IDs
            _writerMock.Verify(w => w.WriteUInt32(500u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(600u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(700u), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new DeleteSubscriptionsRequest
            {
                SubscriptionIds = [999]
            };

            var callOrder = new List<string>();

            // NodeId (TypeID) -> Header -> Array
            _writerMock.Setup(w => w.WriteUInt16(847))
                       .Callback(() => callOrder.Add("TypeID"));

            _writerMock.Setup(w => w.WriteInt32(1))
                       .Callback(() => callOrder.Add("ArrayLength"));

            _writerMock.Setup(w => w.WriteUInt32(999))
                       .Callback(() => callOrder.Add("SubscriptionID"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int typeIdx = callOrder.IndexOf("TypeID");
            int lengthIdx = callOrder.IndexOf("ArrayLength");
            int idIdx = callOrder.IndexOf("SubscriptionID");

            Assert.True(typeIdx < lengthIdx);
            Assert.True(lengthIdx < idIdx);
        }
    }
}