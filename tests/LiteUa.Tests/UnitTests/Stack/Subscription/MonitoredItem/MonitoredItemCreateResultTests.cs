using LiteUa.Encoding;
using LiteUa.Stack.Subscription.MonitoredItem;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Subscription.MonitoredItem
{
    [Trait("Category", "Unit")]
    public class MonitoredItemCreateResultTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public MonitoredItemCreateResultTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidStream_SetsAllProperties()
        {
            // Arrange
            uint expectedItemId = 1001;
            double expectedSampling = 500.5;
            uint expectedQueue = 10;

            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)            // StatusCode
                .Returns(expectedItemId) // MonitoredItemId
                .Returns(expectedQueue); // RevisedQueueSize

            // 2. Setup Double for RevisedSamplingInterval
            _readerMock.Setup(r => r.ReadDouble()).Returns(expectedSampling);

            // Act
            var result = MonitoredItemCreateResult.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.StatusCode.IsGood);
            Assert.Equal(expectedItemId, result.MonitoredItemId);
            Assert.Equal(expectedSampling, result.RevisedSamplingInterval);
            Assert.Equal(expectedQueue, result.RevisedQueueSize);
            Assert.NotNull(result.FilterResult);
            Assert.Equal(0x00, result.FilterResult.Encoding);
        }

        [Fact]
        public void Decode_BadStatusCode_StillParsesRemainingFields()
        {
            // Arrange
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0x803E0000u) // StatusCode
                .Returns(10u)          // MonitoredItemId
                .Returns(90u);         // RevisedQueueSize

            // Act
            var result = MonitoredItemCreateResult.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.StatusCode.IsBad);
            Assert.Equal(10u, result.MonitoredItemId);
            Assert.Equal(90u, result.RevisedQueueSize);
        }

        [Fact]
        public void Decode_VerifiesFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() =>
            {
                callOrder.Add("UInt32");
                return 0u;
            });

            _readerMock.Setup(r => r.ReadDouble()).Returns(() =>
            {
                callOrder.Add("Double");
                return 0.0;
            });

            _readerMock.Setup(r => r.ReadByte()).Returns(() =>
            {
                callOrder.Add("Byte");
                return 0;
            });

            // Act
            MonitoredItemCreateResult.Decode(_readerMock.Object);

            // Assert
            // Order: Status (UInt32) -> Id (UInt32) -> Interval (Double) -> Size (UInt32) -> Filter (Byte/Static)
            Assert.Equal("UInt32", callOrder[0]);
            Assert.Equal("UInt32", callOrder[1]);
            Assert.Equal("Double", callOrder[2]);
            Assert.Equal("UInt32", callOrder[3]);
            Assert.Equal("Byte", callOrder[4]);
        }
    }
}