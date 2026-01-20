using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using LiteUa.Stack.Subscription.MonitoredItem;
using LiteUa.Transport;
using LiteUa.Transport.Headers;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class SubscriptionTests
    {
        private readonly Mock<IUaTcpClientChannel> _channelMock;
        private readonly LiteUa.Stack.Subscription.Subscription _sut;

        public SubscriptionTests()
        {
            _channelMock = new Mock<IUaTcpClientChannel>();
            _channelMock.Setup(c => c.CreateRequestHeader()).Returns(new RequestHeader());
            _sut = new LiteUa.Stack.Subscription.Subscription(_channelMock.Object);
        }

        [Fact]
        public async Task CreateAsync_SendsRequest_AndStartsLoop()
        {
            // Arrange
            var expectedId = 99u;
            var response = new CreateSubscriptionResponse
            {
                SubscriptionId = expectedId,
                RevisedPublishingInterval = 1000.0,
                RevisedMaxKeepAliveCount = 10
            };

            _channelMock.Setup(c => c.SendRequestAsync<CreateSubscriptionRequest, CreateSubscriptionResponse>(It.IsAny<CreateSubscriptionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            _channelMock.Setup(c => c.SendRequestAsync<PublishRequest, PublishResponse>(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => new TaskCompletionSource<PublishResponse>().Task);

            // Act
            await _sut.CreateAsync(1000.0);

            // Assert
            _channelMock.Verify(c => c.SendRequestAsync<CreateSubscriptionRequest, CreateSubscriptionResponse>(
                It.Is<CreateSubscriptionRequest>(r => r.RequestedPublishingInterval == 1000.0), It.IsAny<CancellationToken>()), Times.Once);

            // Verify background loop started
            await Task.Delay(100);
            _channelMock.Verify(c => c.SendRequestAsync<PublishRequest, PublishResponse>(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task CreateMonitoredItemsAsync_Throws_OnCountMismatch()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.CreateMonitoredItemsAsync(new NodeId[1], new uint[2]));
        }

        [Fact]
        public async Task CreateMonitoredItemsAsync_MapsResultsCorrectly()
        {
            // Arrange
            var nodeIds = new[] { new NodeId(1), new NodeId(2) };
            var handles = new[] { 10u, 11u };

            var response = new CreateMonitoredItemsResponse
            {
                Results =
                [
                new MonitoredItemCreateResult { StatusCode = new StatusCode(0), MonitoredItemId = 100 },
                new MonitoredItemCreateResult { StatusCode = new StatusCode(0x80000000), MonitoredItemId = 0 } // Bad
            ]
            };

            _channelMock.Setup(c => c.SendRequestAsync<CreateMonitoredItemsRequest, CreateMonitoredItemsResponse>(It.IsAny<CreateMonitoredItemsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var results = await _sut.CreateMonitoredItemsAsync(nodeIds, handles);

            // Assert
            Assert.Equal(2, results.Length);
            Assert.Equal(100u, results[0]);
            Assert.Equal(0u, results[1]);
        }

        [Fact]
        public async Task PublishLoop_DataChange_TriggersEvent()
        {
            // Arrange
            var tcs = new TaskCompletionSource<(uint, DataValue)>();
            _sut.DataChanged += (h, v) => tcs.TrySetResult((h, v));
            _channelMock.Setup(c => c.SendRequestAsync<CreateSubscriptionRequest, CreateSubscriptionResponse>(It.IsAny<CreateSubscriptionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateSubscriptionResponse { SubscriptionId = 1, RevisedPublishingInterval = 100, RevisedMaxKeepAliveCount = 1 });
            var dataChangeBody = CreateDataChangeNotificationBytes(42u, 100.5);

            var pubResponse = new PublishResponse
            {
                SubscriptionId = 1,
                NotificationMessage = new NotificationMessage
                {
                    SequenceNumber = 1,
                    NotificationData =
                    [
                    new ExtensionObject { TypeId = new NodeId(811), Encoding = 0x01, Body = dataChangeBody }
                ]
                }
            };

            _channelMock.SetupSequence(c => c.SendRequestAsync<PublishRequest, PublishResponse>(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pubResponse)
                .Returns(() => new TaskCompletionSource<PublishResponse>().Task);

            // Act
            await _sut.CreateAsync();

            // Assert
            var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(42u, result.Item1);
            Assert.Equal(100.5, result.Item2.Value?.Value);
        }

        [Fact]
        public async Task PublishLoop_StatusChangeBad_TriggersConnectionLost()
        {
            // Arrange
            var tcs = new TaskCompletionSource<Exception>();
            _sut.ConnectionLost += (ex) => tcs.TrySetResult(ex);

            _channelMock.Setup(c => c.SendRequestAsync<CreateSubscriptionRequest, CreateSubscriptionResponse>(It.IsAny<CreateSubscriptionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateSubscriptionResponse { SubscriptionId = 1 });

            var statusChangeBody = CreateStatusChangeNotificationBytes(0x80000000u); // Bad

            var pubResponse = new PublishResponse
            {
                NotificationMessage = new NotificationMessage
                {
                    NotificationData =
                    [
                    new ExtensionObject { TypeId = new NodeId(820), Encoding = 0x01, Body = statusChangeBody }
                ]
                }
            };

            _channelMock.Setup(c => c.SendRequestAsync<PublishRequest, PublishResponse>(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pubResponse);

            // Act
            await _sut.CreateAsync();

            // Assert
            var ex = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Contains("terminated by Server", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_StopsLoop_AndSendsRequest()
        {
            // Arrange
            _channelMock.Setup(c => c.SendRequestAsync<CreateSubscriptionRequest, CreateSubscriptionResponse>(It.IsAny<CreateSubscriptionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateSubscriptionResponse { SubscriptionId = 500 });

            _channelMock.Setup(c => c.SendRequestAsync<PublishRequest, PublishResponse>(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => new TaskCompletionSource<PublishResponse>().Task);

            await _sut.CreateAsync();

            // Act
            await _sut.DeleteAsync();

            // Assert
            _channelMock.Verify(c => c.DeleteSubscriptionsAsync(It.Is<uint[]>(ids => ids[0] == 500), It.IsAny<CancellationToken>()), Times.Once);
        }

        private static byte[] CreateDataChangeNotificationBytes(uint handle, double value)
        {
            using var ms = new System.IO.MemoryStream();
            var w = new OpcUaBinaryWriter(ms);

            // 1. MonitoredItems Array
            w.WriteInt32(1); // Count = 1
            w.WriteUInt32(handle); // MonitoredItemNotification.ClientHandle

            // DataValue.Decode logic:
            w.WriteByte(0x01); // Mask: Value present

            // Variant.Decode logic:
            w.WriteByte(0x0B); // BuiltInType: Double (11)
            w.WriteDouble(value); // The actual 8-byte value

            // 2. DiagnosticInfos Array
            w.WriteInt32(0); // Count = 0 (Empty array)

            return ms.ToArray();
        }

        private static byte[] CreateStatusChangeNotificationBytes(uint statusCode)
        {
            using var ms = new System.IO.MemoryStream();
            var w = new OpcUaBinaryWriter(ms);
            w.WriteUInt32(statusCode);
            w.WriteByte(0x00); // No Diags
            return ms.ToArray();
        }
    }
}
