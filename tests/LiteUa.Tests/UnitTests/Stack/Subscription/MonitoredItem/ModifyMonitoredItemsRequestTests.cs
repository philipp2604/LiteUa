using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using LiteUa.Stack.Subscription.MonitoredItem;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription.MonitoredItem
{
    [Trait("Category", "Unit")]
    public class ModifyMonitoredItemsRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public ModifyMonitoredItemsRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(763u, ModifyMonitoredItemsRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var request = new ModifyMonitoredItemsRequest();

            // Assert
            Assert.Equal(TimestampsToReturn.Both, request.TimestampsToReturn);
            Assert.NotNull(request.RequestHeader);
            Assert.Equal(0u, request.SubscriptionId);
        }

        [Fact]
        public void Encode_NullItemsToModify_WritesNegativeOne()
        {
            // Arrange
            var request = new ModifyMonitoredItemsRequest
            {
                SubscriptionId = 12345,
                ItemsToModify = null
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt32(12345u), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithItems_WritesCorrectSequence()
        {
            // Arrange
            var item = new MonitoredItemModifyRequest(0, new());
            var request = new ModifyMonitoredItemsRequest
            {
                SubscriptionId = 88,
                TimestampsToReturn = TimestampsToReturn.Server,
                ItemsToModify = new[] { item }
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. Service ID (763)
            _writerMock.Verify(w => w.WriteUInt16(763), Times.Once);

            // 2. SubscriptionId
            _writerMock.Verify(w => w.WriteUInt32(88u), Times.Once);

            // 3. TimestampsToReturn (Server = 1)
            _writerMock.Verify(w => w.WriteUInt32(1u), Times.Once);

            // 4. Array Length (1)
            _writerMock.Verify(w => w.WriteInt32(1), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new ModifyMonitoredItemsRequest
            {
                SubscriptionId = 9999,
                TimestampsToReturn = TimestampsToReturn.Neither, // 3
                ItemsToModify = new[] { new MonitoredItemModifyRequest(0, new()) }
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteUInt32(9999))
                       .Callback(() => callOrder.Add("SubId"));

            _writerMock.Setup(w => w.WriteUInt32(3u))
                       .Callback(() => callOrder.Add("Timestamps"));

            _writerMock.Setup(w => w.WriteInt32(1))
                       .Callback(() => callOrder.Add("ArrayLength"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int subIdx = callOrder.IndexOf("SubId");
            int timeIdx = callOrder.IndexOf("Timestamps");
            int arrayIdx = callOrder.IndexOf("ArrayLength");

            Assert.True(subIdx != -1);
            Assert.True(timeIdx != -1);
            Assert.True(subIdx < timeIdx);
            Assert.True(timeIdx < arrayIdx);
        }
    }
}
