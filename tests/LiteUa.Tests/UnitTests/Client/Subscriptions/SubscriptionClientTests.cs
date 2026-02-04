using LiteUa.BuiltIn;
using LiteUa.Client.Subscriptions;
using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Stack.Subscription;
using LiteUa.Stack.Subscription.MonitoredItem;
using LiteUa.Transport;
using LiteUa.Transport.Headers;
using Moq;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Tests.UnitTests.Client.Subscriptions
{
    [Trait("Category", "Unit")]
    public class SubscriptionClientTests
    {
        private readonly Mock<IUaTcpClientChannelFactory> _factoryMock;
        private readonly Mock<IUaTcpClientChannel> _channelMock;
        private readonly Mock<IUserIdentity> _userIdentityMock;
        private readonly Mock<ISecurityPolicyFactory> _policyMock;

        public SubscriptionClientTests()
        {
            _factoryMock = new Mock<IUaTcpClientChannelFactory>();
            _channelMock = new Mock<IUaTcpClientChannel>();
            _userIdentityMock = new Mock<IUserIdentity>();
            _policyMock = new Mock<ISecurityPolicyFactory>();

            _channelMock.Setup(c => c.CreateRequestHeader()).Returns(new RequestHeader());

            _channelMock.Setup(c => c.SendRequestAsync<CreateSubscriptionRequest, CreateSubscriptionResponse>(It.IsAny<CreateSubscriptionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateSubscriptionResponse
                {
                    SubscriptionId = 123,
                    RevisedPublishingInterval = 1000,
                    RevisedMaxKeepAliveCount = 10
                });

            _channelMock.Setup(c => c.SendRequestAsync<CreateMonitoredItemsRequest, CreateMonitoredItemsResponse>(It.IsAny<CreateMonitoredItemsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateMonitoredItemsResponse
                {
                    Results = [new MonitoredItemCreateResult { StatusCode = new StatusCode(0), MonitoredItemId = 1 }]
                });

            _channelMock.Setup(c => c.SendRequestAsync<PublishRequest, PublishResponse>(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PublishResponse
                {
                    SubscriptionId = 123,
                    NotificationMessage = new NotificationMessage() // Empty message acts as KeepAlive
                });

            _factoryMock.Setup(f => f.CreateTcpClientChannel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ISecurityPolicyFactory>(), It.IsAny<MessageSecurityMode>(), It.IsAny<X509Certificate2>(), It.IsAny<X509Certificate2>(), It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns(_channelMock.Object);
        }

        private SubscriptionClient CreateSut(uint supervisorMs = 10, uint reconnectMs = 10)
        {
            return new SubscriptionClient(
                "opc.tcp://localhost:4840", "urn:test:client", "urn:test:prod", "TestApp",
                _userIdentityMock.Object, _policyMock.Object, MessageSecurityMode.None,
                null, null, 20000, 10000, 3, 2.0, 10000, _factoryMock.Object, supervisorMs, reconnectMs);
        }

        [Fact]
        public async Task Lifecycle_ConnectsAndReportsStatus()
        {
            // Arrange
            using var sut = CreateSut();
            bool isConnected = false;
            var statusSignal = new SemaphoreSlim(0);

            sut.ConnectionStatusChanged += (status) =>
            {
                isConnected = status;
                statusSignal.Release();
            };

            // Act
            sut.Start();

            // Wait for status changed event
            await statusSignal.WaitAsync(1000);

            // Assert
            Assert.True(isConnected);
            _channelMock.Verify(c => c.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
            _channelMock.Verify(c => c.CreateSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _channelMock.Verify(c => c.ActivateSessionAsync(_userIdentityMock.Object, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SubscribeAsync_GeneratesSequentialHandles()
        {
            // Arrange
            using var sut = CreateSut();
            sut.Start();
            // Wait for connection to be established
            await Task.Delay(50);

            var nodesA = new[] { new NodeId(1, 100u) };
            var nodesB = new[] { new NodeId(1, 101u), new NodeId(1, 102u) };

            // Act
            var handlesA = await sut.SubscribeAsync(nodesA);
            var handlesB = await sut.SubscribeAsync(nodesB);

            // Assert
            Assert.Equal(1u, handlesA[0]);
            Assert.Equal(2u, handlesB[0]);
            Assert.Equal(3u, handlesB[1]);
        }

        [Fact]
        public async Task SubscribeAsync_WaitingForConnection_BlocksAndThenExecutes()
        {
            // Arrange
            using var sut = CreateSut(supervisorMs: 100); // Slower connection
            var nodes = new[] { new NodeId(1, 999u) };

            // Act
            var subscribeTask = sut.SubscribeAsync(nodes);
            Assert.False(subscribeTask.IsCompleted); // Should be blocked by TaskCompletionSource

            sut.Start();

            // Assert
            var handles = await subscribeTask;
            Assert.NotNull(handles);
            Assert.Single(handles);
        }

        [Fact]
        public async Task SupervisorLoop_RetriesConnection_OnFailure()
        {
            // Arrange
            int connectionAttempts = 0;
            _channelMock.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    connectionAttempts++;
                    return (connectionAttempts == 1)
                        ? Task.FromException(new Exception("Network Fail"))
                        : Task.CompletedTask;
                });

            using var sut = CreateSut(supervisorMs: 10, reconnectMs: 10);

            // Act
            sut.Start();
            await Task.Delay(100); // Allow time for failure and retry

            // Assert
            Assert.True(connectionAttempts >= 2);
            _channelMock.Verify(c => c.ActivateSessionAsync(It.IsAny<IUserIdentity>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Reconnection_RestoresSubscriptions()
        {
            // Arrange
            using var sut = CreateSut();
            sut.Start();
            await Task.Delay(50); // Connect

            await sut.SubscribeAsync([new NodeId(1, 1u)], 500.0);

            // Act: Simulate connection loss
            var triggerMethod = typeof(SubscriptionClient).GetMethod("TriggerReconnect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            triggerMethod?.Invoke(sut, null);

            await Task.Delay(100); // Wait for restoration

            // Assert:
            // 1. Old channel disposed
            _channelMock.Verify(c => c.DisposeAsync(), Times.AtLeastOnce);
            // 2. New channel setup
            _channelMock.Verify(c => c.CreateSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            // 3. RestoreItemsAsync triggers a re-subscription by sending a CreateMonitoredItemsRequest
            _channelMock.Verify(c => c.SendRequestAsync<CreateMonitoredItemsRequest, CreateMonitoredItemsResponse>(It.IsAny<CreateMonitoredItemsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task OnDataChanged_FiresPublicEvent()
        {
            // Arrange
            using var sut = CreateSut();
            uint receivedHandle = 0;
            DataValue? receivedValue = null;
            sut.DataChanged += (h, v) => { receivedHandle = h; receivedValue = v; };

            var testValue = new DataValue { Value = new Variant("Test", BuiltInType.String) };

            // Act: Trigger private callback
            var method = typeof(SubscriptionClient).GetMethod("OnDataChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(sut, [42u, testValue]);

            // Assert
            Assert.Equal(42u, receivedHandle);
            Assert.Equal("Test", receivedValue?.Value?.Value);
        }

        [Fact]
        public async Task Dispose_StopsAllTasksAndCleansUp()
        {
            // Arrange
            var sut = CreateSut();
            sut.Start();
            await Task.Delay(500);

            // Act
            await sut.DisposeAsync();

            // Assert
            // Verify channel was disposed
            _channelMock.Verify(c => c.DisposeAsync(), Times.AtLeastOnce);

            // Ensure no further connection attempts (Wait and verify count stays same)
            var countAfterDispose = _channelMock.Invocations.Count(i => i.Method.Name == "ConnectAsync");
            await Task.Delay(100);
            var countLater = _channelMock.Invocations.Count(i => i.Method.Name == "ConnectAsync");
            Assert.Equal(countAfterDispose, countLater);
        }
    }
}