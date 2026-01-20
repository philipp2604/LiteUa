using LiteUa.Encoding;
using LiteUa.Stack.Subscription.MonitoredItem;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription.MonitoredItem
{
    [Trait("Category", "Unit")]
    public class MonitoredItemModifyResultTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public MonitoredItemModifyResultTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidStream_SetsAllProperties()
        {
            // Arrange
            double expectedSampling = 250.0;
            uint expectedQueue = 50;

            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)            // StatusCode
                .Returns(expectedQueue); // RevisedQueueSize
            _readerMock.Setup(r => r.ReadDouble()).Returns(expectedSampling); // RevisedSamplingInterval

            // Act
            var result = MonitoredItemModifyResult.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.StatusCode.IsGood);
            Assert.Equal(expectedSampling, result.RevisedSamplingInterval);
            Assert.Equal(expectedQueue, result.RevisedQueueSize);
            Assert.NotNull(result.FilterResult);
            Assert.Equal(0x00, result.FilterResult.Encoding);
        }

        [Fact]
        public void Decode_BadStatusCode_StillParsesRemainingFields()
        {
            // Arrange
            // "Bad_SubscriptionIdInvalid" status (0x80280000)
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0x80280000u) // StatusCode
                .Returns(220u);          // RevisedQueueSize

            // Act
            var result = MonitoredItemModifyResult.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.StatusCode.IsBad);
            Assert.Equal(220u, result.RevisedQueueSize);
        }

        [Fact]
        public void Decode_VerifiesFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();
            _readerMock.Setup(r => r.ReadUInt32()).Returns(() => {
                callOrder.Add("UInt32");
                return 0u;
            });

            _readerMock.Setup(r => r.ReadDouble()).Returns(() => {
                callOrder.Add("Double");
                return 0.0;
            });

            _readerMock.Setup(r => r.ReadByte()).Returns(() => {
                callOrder.Add("Byte");
                return 0;
            });

            // Act
            MonitoredItemModifyResult.Decode(_readerMock.Object);

            // Assert
            // Status (UInt32) -> RevisedInterval (Double) -> RevisedQueueSize (UInt32) -> FilterResult (ExtObj)
            Assert.Equal("UInt32", callOrder[0]); // StatusCode
            Assert.Equal("Double", callOrder[1]); // RevisedSamplingInterval
            Assert.Equal("UInt32", callOrder[2]); // RevisedQueueSize
            Assert.Equal("Byte", callOrder[3]); // ExtensionObject Type/Mask start
        }
    }
}
