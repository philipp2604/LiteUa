using LiteUa.Encoding;
using LiteUa.Stack.Subscription.MonitoredItem;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Subscription.MonitoredItem
{
    [Trait("Category", "Unit")]
    public class MonitoredItemModifyRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public MonitoredItemModifyRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            uint expectedId = 12345;
            var parameters = new MonitoringParameters { ClientHandle = 42 };

            // Act
            var request = new MonitoredItemModifyRequest(expectedId, parameters);

            // Assert
            Assert.Equal(expectedId, request.MonitoredItemId);
            Assert.Same(parameters, request.RequestedParameters);
        }

        [Fact]
        public void Encode_WritesCorrectValuesInSequence()
        {
            // Arrange
            uint id = 999;
            var parameters = new MonitoringParameters { ClientHandle = 88 };
            var request = new MonitoredItemModifyRequest(id, parameters);

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt32(999u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(88u), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            uint id = 5555;
            var parameters = new MonitoringParameters { ClientHandle = 1111 };
            var request = new MonitoredItemModifyRequest(id, parameters);

            var callOrder = new List<string>();

            _writerMock.Setup(w => w.WriteUInt32(5555))
                       .Callback(() => callOrder.Add("ItemId"));

            _writerMock.Setup(w => w.WriteUInt32(1111))
                       .Callback(() => callOrder.Add("Parameters"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // Order: MonitoredItemId -> MonitoringParameters
            Assert.Equal("ItemId", callOrder[0]);
            Assert.Equal("Parameters", callOrder[1]);
        }
    }
}