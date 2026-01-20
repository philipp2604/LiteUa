using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class SetMonitoringModeRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public SetMonitoringModeRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(769u, SetMonitoringModeRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_InitializesDefaults()
        {
            // Act
            var request = new SetMonitoringModeRequest();

            // Assert
            Assert.NotNull(request.RequestHeader);
            Assert.Equal(0u, request.SubscriptionId);
            Assert.Equal(0u, request.MonitoringMode);
            Assert.Null(request.MonitoredItemIds);
        }

        [Fact]
        public void Encode_NullMonitoredItemIds_WritesNegativeOne()
        {
            // Arrange
            var request = new SetMonitoringModeRequest
            {
                SubscriptionId = 12345,
                MonitoringMode = 2, // Reporting
                MonitoredItemIds = null
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt16(769), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(12345u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(2u), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithIds_WritesCountAndElements()
        {
            // Arrange
            uint[] ids = [10, 20, 30];
            var request = new SetMonitoringModeRequest
            {
                SubscriptionId = 55,
                MonitoringMode = 1, // Sampling
                MonitoredItemIds = ids
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteInt32(3), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(10u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(20u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(30u), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new SetMonitoringModeRequest
            {
                SubscriptionId = 9999,
                MonitoringMode = 8888,
                MonitoredItemIds = [7777]
            };

            var callOrder = new List<string>();

            _writerMock.Setup(w => w.WriteUInt32(9999))
                       .Callback(() => callOrder.Add("SubId"));

            _writerMock.Setup(w => w.WriteUInt32(8888))
                       .Callback(() => callOrder.Add("Mode"));

            _writerMock.Setup(w => w.WriteInt32(1))
                       .Callback(() => callOrder.Add("ArrayLength"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int subIdx = callOrder.IndexOf("SubId");
            int modeIdx = callOrder.IndexOf("Mode");
            int lengthIdx = callOrder.IndexOf("ArrayLength");

            Assert.True(subIdx != -1);
            Assert.True(modeIdx != -1);
            Assert.True(subIdx < modeIdx);
            Assert.True(modeIdx < lengthIdx);
        }
    }
}
