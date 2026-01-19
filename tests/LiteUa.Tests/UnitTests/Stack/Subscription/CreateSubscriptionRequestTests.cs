using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class CreateSubscriptionRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public CreateSubscriptionRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(787u, CreateSubscriptionRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_SetsStandardOpcUaDefaults()
        {
            // Act
            var request = new CreateSubscriptionRequest();

            // Assert
            Assert.Equal(1000.0, request.RequestedPublishingInterval);
            Assert.Equal(60u, request.RequestedLifetimeCount);
            Assert.Equal(20u, request.RequestedMaxKeepAliveCount);
            Assert.Equal(0u, request.MaxNotificationsPerPublish);
            Assert.True(request.PublishingEnabled);
            Assert.Equal(0, request.Priority);
            Assert.NotNull(request.RequestHeader);
        }

        [Fact]
        public void Encode_WritesAllFieldsWithCorrectValues()
        {
            // Arrange
            var request = new CreateSubscriptionRequest
            {
                RequestedPublishingInterval = 500.5,
                RequestedLifetimeCount = 30,
                RequestedMaxKeepAliveCount = 10,
                MaxNotificationsPerPublish = 100,
                PublishingEnabled = false,
                Priority = 5
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. Service ID
            _writerMock.Verify(w => w.WriteUInt16(787), Times.Once);

            // 2. Data Fields
            _writerMock.Verify(w => w.WriteDouble(500.5), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(30u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(10u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(100u), Times.Once);
            _writerMock.Verify(w => w.WriteBoolean(false), Times.Once);
            _writerMock.Verify(w => w.WriteByte(5), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesExactFieldOrder()
        {
            // Arrange
            var request = new CreateSubscriptionRequest
            {
                RequestedPublishingInterval = 123.456,
                RequestedLifetimeCount = 999,
                RequestedMaxKeepAliveCount = 888,
                MaxNotificationsPerPublish = 777,
                PublishingEnabled = true,
                Priority = 123
            };

            var callOrder = new List<string>();

            _writerMock.Setup(w => w.WriteDouble(123.456))
                       .Callback(() => callOrder.Add("Interval"));

            _writerMock.Setup(w => w.WriteUInt32(999))
                       .Callback(() => callOrder.Add("Lifetime"));

            _writerMock.Setup(w => w.WriteUInt32(888))
                       .Callback(() => callOrder.Add("KeepAlive"));

            _writerMock.Setup(w => w.WriteUInt32(777))
                       .Callback(() => callOrder.Add("Notifications"));

            _writerMock.Setup(w => w.WriteBoolean(true))
                       .Callback(() => callOrder.Add("Enabled"));

            _writerMock.Setup(w => w.WriteByte(123))
                       .Callback(() => callOrder.Add("Priority"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            Assert.Equal("Interval", callOrder[0]);
            Assert.Equal("Lifetime", callOrder[1]);
            Assert.Equal("KeepAlive", callOrder[2]);
            Assert.Equal("Notifications", callOrder[3]);
            Assert.Equal("Enabled", callOrder[4]);
            Assert.Equal("Priority", callOrder[5]);
        }

        [Fact]
        public void Encode_TriggersRequestHeaderEncode()
        {
            // Arrange
            var request = new CreateSubscriptionRequest();

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt32(It.IsAny<uint>()), Times.AtLeast(2));
        }
    }
}
