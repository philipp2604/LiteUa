using LiteUa.Encoding;
using LiteUa.Stack.Subscription.MonitoredItem;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription.MonitoredItem
{
    [Trait("Category", "Unit")]
    public class DeleteMonitoredItemsRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public DeleteMonitoredItemsRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(781u, DeleteMonitoredItemsRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Encode_NullMonitoredItemIds_WritesNegativeOne()
        {
            // Arrange
            var request = new DeleteMonitoredItemsRequest
            {
                SubscriptionId = 12345,
                MonitoredItemIds = null
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt32(12345u), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithItems_WritesCountAndElements()
        {
            // Arrange
            uint[] ids = { 101, 102 };
            var request = new DeleteMonitoredItemsRequest
            {
                SubscriptionId = 55,
                MonitoredItemIds = ids
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. Service ID
            _writerMock.Verify(w => w.WriteUInt16(781), Times.Once);

            // 2. SubscriptionId
            _writerMock.Verify(w => w.WriteUInt32(55u), Times.Once);

            // 3. Array Length (2)
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once);

            // 4. Individual IDs
            _writerMock.Verify(w => w.WriteUInt32(101u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(102u), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new DeleteMonitoredItemsRequest
            {
                SubscriptionId = 9999,
                MonitoredItemIds = new uint[] { 1 }
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteUInt32(9999))
                       .Callback(() => callOrder.Add("SubId"));

            _writerMock.Setup(w => w.WriteInt32(1))
                       .Callback(() => callOrder.Add("ArrayLength"));

            _writerMock.Setup(w => w.WriteUInt32(1))
                       .Callback(() => callOrder.Add("ItemId"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int subIdx = callOrder.IndexOf("SubId");
            int lengthIdx = callOrder.IndexOf("ArrayLength");
            int itemIdx = callOrder.IndexOf("ItemId");

            Assert.True(subIdx < lengthIdx);
            Assert.True(lengthIdx < itemIdx);
        }
    }
}
