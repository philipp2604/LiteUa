using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class PublishRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public PublishRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(826u, PublishRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var request = new PublishRequest();

            // Assert
            Assert.NotNull(request.RequestHeader);
            Assert.NotNull(request.SubscriptionAcknowledgements);
            Assert.Empty(request.SubscriptionAcknowledgements);
        }

        [Fact]
        public void Encode_NullAcknowledgements_WritesNegativeOne()
        {
            // Arrange
            var request = new PublishRequest
            {
                SubscriptionAcknowledgements = null!
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt16(826), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithAcknowledgements_WritesCountAndDelegates()
        {
            // Arrange
            var ack1 = new SubscriptionAcknowledgement { SequenceNumber = 10, SubscriptionId = 1 };
            var ack2 = new SubscriptionAcknowledgement { SequenceNumber = 11, SubscriptionId = 1 };

            var request = new PublishRequest
            {
                SubscriptionAcknowledgements = [ack1, ack2]
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(10u), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(11u), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new PublishRequest
            {
                SubscriptionAcknowledgements = [new SubscriptionAcknowledgement { SequenceNumber = 999 }]
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteUInt16(826))
                       .Callback(() => callOrder.Add("ServiceID"));

            _writerMock.Setup(w => w.WriteInt32(1))
                       .Callback(() => callOrder.Add("ArrayLength"));

            _writerMock.Setup(w => w.WriteUInt32(999))
                       .Callback(() => callOrder.Add("AckPayload"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int serviceIdx = callOrder.IndexOf("ServiceID");
            int lengthIdx = callOrder.IndexOf("ArrayLength");
            int payloadIdx = callOrder.IndexOf("AckPayload");
            Assert.True(serviceIdx < lengthIdx);
            Assert.True(lengthIdx < payloadIdx);
        }
    }
}
