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
    public class CreateMonitoredItemsRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public CreateMonitoredItemsRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(751u, CreateMonitoredItemsRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var request = new CreateMonitoredItemsRequest();

            // Assert
            Assert.Equal(2u, (uint)request.TimestampsToReturn); // Both
            Assert.NotNull(request.RequestHeader);
            Assert.Equal(0u, request.SubscriptionId);
        }

        [Fact]
        public void Encode_NullItemsToCreate_WritesNegativeOne()
        {
            // Arrange
            var request = new CreateMonitoredItemsRequest
            {
                ItemsToCreate = null,
                SubscriptionId = 123
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt32(123u), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithItems_WritesCorrectSequence()
        {
            // Arrange
            var request = new CreateMonitoredItemsRequest
            {
                SubscriptionId = 55,
                TimestampsToReturn = TimestampsToReturn.Server,
                ItemsToCreate = new[] { new MonitoredItemCreateRequest(new ReadValueId(new(123)), 456, new()) }
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. Service ID (751)
            _writerMock.Verify(w => w.WriteUInt16(751), Times.Once);

            // 2. Subscription ID
            _writerMock.Verify(w => w.WriteUInt32(55u), Times.Once);

            // 3. TimestampsToReturn
            _writerMock.Verify(w => w.WriteUInt32(1u), Times.Once);

            // 4. Array Length (1)
            _writerMock.Verify(w => w.WriteInt32(1), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new CreateMonitoredItemsRequest
            {
                SubscriptionId = 9999,
                TimestampsToReturn = TimestampsToReturn.Neither,
                ItemsToCreate = new[] { new MonitoredItemCreateRequest(new ReadValueId(new(123)), 456, new()) } 
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteUInt32(9999))
                       .Callback(() => callOrder.Add("SubId"));

            _writerMock.Setup(w => w.WriteUInt32((uint)TimestampsToReturn.Neither))
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
