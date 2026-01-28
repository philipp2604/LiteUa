using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using LiteUa.Stack.Subscription.MonitoredItem;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Subscription.MonitoredItem
{
    [Trait("Category", "Unit")]
    public class MonitoredItemCreateRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public MonitoredItemCreateRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var readValue = new ReadValueId(new NodeId(101));
            var parameters = new MonitoringParameters { ClientHandle = 42 };
            uint mode = 2; // Reporting

            // Act
            var request = new MonitoredItemCreateRequest(readValue, mode, parameters);

            // Assert
            Assert.Same(readValue, request.ItemToMonitor);
            Assert.Equal(mode, request.MonitoringMode);
            Assert.Same(parameters, request.RequestedParameters);
        }

        [Fact]
        public void Encode_WritesCorrectValuesInSequence()
        {
            // Arrange
            var readValue = new ReadValueId(new NodeId(0, 50u)) { AttributeId = 13 };
            var parameters = new MonitoringParameters { ClientHandle = 99 };
            uint mode = 1; // Sampling

            var request = new MonitoredItemCreateRequest(readValue, mode, parameters);

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. ReadValueId part (Numeric 50 + Attr 13)
            _writerMock.Verify(w => w.WriteByte(50), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(13u), Times.Once);

            // 2. MonitoringMode
            _writerMock.Verify(w => w.WriteUInt32(1u), Times.Once);

            // 3. MonitoringParameters part (ClientHandle 99)
            _writerMock.Verify(w => w.WriteUInt32(99u), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var readValue = new ReadValueId(new NodeId(0)) { AttributeId = 777 };
            var parameters = new MonitoringParameters { ClientHandle = 888 };
            uint mode = 555;

            var request = new MonitoredItemCreateRequest(readValue, mode, parameters);
            var callOrder = new List<string>();

            _writerMock.Setup(w => w.WriteUInt32(777)).Callback(() => callOrder.Add("ItemToMonitor"));
            _writerMock.Setup(w => w.WriteUInt32(555)).Callback(() => callOrder.Add("Mode"));
            _writerMock.Setup(w => w.WriteUInt32(888)).Callback(() => callOrder.Add("Parameters"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            Assert.Equal("ItemToMonitor", callOrder[0]);
            Assert.Equal("Mode", callOrder[1]);
            Assert.Equal("Parameters", callOrder[2]);
        }
    }
}