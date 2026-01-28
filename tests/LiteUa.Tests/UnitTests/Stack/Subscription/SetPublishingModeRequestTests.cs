using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class SetPublishingModeRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public SetPublishingModeRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(799u, SetPublishingModeRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_InitializesDefaults()
        {
            // Act
            var request = new SetPublishingModeRequest();

            // Assert
            Assert.NotNull(request.RequestHeader);
            Assert.False(request.PublishingEnabled);
            Assert.Null(request.SubscriptionIds);
        }

        [Fact]
        public void Encode_NullSubscriptionIds_WritesNegativeOne()
        {
            // Arrange
            var request = new SetPublishingModeRequest
            {
                PublishingEnabled = true,
                SubscriptionIds = null
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt16(799), Times.Once);
            _writerMock.Verify(w => w.WriteBoolean(true), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithIds_WritesCountAndElements()
        {
            // Arrange
            uint[] ids = [1001, 1002];
            var request = new SetPublishingModeRequest
            {
                PublishingEnabled = false,
                SubscriptionIds = ids
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteBoolean(false), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(1001u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(1002u), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new SetPublishingModeRequest
            {
                PublishingEnabled = true,
                SubscriptionIds = [999]
            };

            var callOrder = new List<string>();

            // NodeId (TypeID) -> Boolean -> Array length
            _writerMock.Setup(w => w.WriteUInt16(799))
                       .Callback(() => callOrder.Add("TypeID"));

            _writerMock.Setup(w => w.WriteBoolean(true))
                       .Callback(() => callOrder.Add("EnabledFlag"));

            _writerMock.Setup(w => w.WriteInt32(1))
                       .Callback(() => callOrder.Add("ArrayLength"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int typeIdx = callOrder.IndexOf("TypeID");
            int boolIdx = callOrder.IndexOf("EnabledFlag");
            int lengthIdx = callOrder.IndexOf("ArrayLength");

            Assert.True(typeIdx < boolIdx);
            Assert.True(boolIdx < lengthIdx);
        }
    }
}