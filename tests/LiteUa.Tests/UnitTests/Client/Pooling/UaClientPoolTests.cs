using LiteUa.Client.Pooling;
using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LiteUa.Tests.UnitTests.Client.Pooling
{
    [Trait("Category", "Unit")]
    public class UaClientPoolTests
    {
        private readonly Mock<IUaTcpClientChannelFactory> _factoryMock;
        private readonly Mock<IUaTcpClientChannel> _channelMock;
        private readonly Mock<IUserIdentity> _userMock;
        private readonly Mock<ISecurityPolicyFactory> _policyMock;

        private readonly UaClientPool _pool;

        public UaClientPoolTests()
        {
            _factoryMock = new Mock<IUaTcpClientChannelFactory>();
            _channelMock = new Mock<IUaTcpClientChannel>();
            _userMock = new Mock<IUserIdentity>();
            _policyMock = new Mock<ISecurityPolicyFactory>();

            // Default factory setup
            _factoryMock.Setup(f => f.CreateTcpClientChannel(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ISecurityPolicyFactory>(), It.IsAny<MessageSecurityMode>(),
                It.IsAny<X509Certificate2>(), It.IsAny<X509Certificate2>()))
                .Returns(_channelMock.Object);

            _pool = new UaClientPool(
                "opc.tcp://localhost:4840", "uri", "prod", "app",
                _userMock.Object, _policyMock.Object, MessageSecurityMode.None,
                null, null, 2, _factoryMock.Object); // Max size 2
        }

        [Fact]
        public async Task RentAsync_PoolEmpty_CreatesAndConnectsNewClient()
        {
            // Act
            var pooledClient = await _pool.RentAsync();

            // Assert
            Assert.NotNull(pooledClient);
            Assert.Equal(_channelMock.Object, pooledClient.InnerClient);

            // Verify initialization sequence
            _channelMock.Verify(c => c.ConnectAsync(default), Times.Once);
            _channelMock.Verify(c => c.CreateSessionAsync(It.IsAny<string>()), Times.Once);
            _channelMock.Verify(c => c.ActivateSessionAsync(_userMock.Object), Times.Once);
        }

        [Fact]
        public async Task RentAsync_ReturnsToPool_ReusesSameClient()
        {
            // 1. Rent
            var firstRent = await _pool.RentAsync();
            var firstInternalClient = firstRent.InnerClient;

            // 2. Return (via Dispose)
            firstRent.Dispose();

            // 3. Rent again
            var secondRent = await _pool.RentAsync();

            // Assert
            Assert.Same(firstInternalClient, secondRent.InnerClient);

            // Factory should only have been called once
            _factoryMock.Verify(f => f.CreateTcpClientChannel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ISecurityPolicyFactory>(), It.IsAny<MessageSecurityMode>(),
                It.IsAny<X509Certificate2>(), It.IsAny<X509Certificate2>()), Times.Once);
        }

        [Fact]
        public async Task Return_InvalidClient_DisposesAndDoesNotReuse()
        {
            // Arrange
            var pooledClient = await _pool.RentAsync();
            pooledClient.IsInvalid = true;

            // Act
            pooledClient.Dispose(); // Triggers pool.Return

            // Assert
            _channelMock.Verify(c => c.Dispose(), Times.Once);

            // Try to rent again
            await _pool.RentAsync();
            _factoryMock.Verify(f => f.CreateTcpClientChannel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ISecurityPolicyFactory>(), It.IsAny<MessageSecurityMode>(),
                It.IsAny<X509Certificate2>(), It.IsAny<X509Certificate2>()), Times.Exactly(2));
        }

        [Fact]
        public async Task RentAsync_FactoryFails_ReleasesSemaphore()
        {
            // Arrange
            _factoryMock.Setup(f => f.CreateTcpClientChannel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ISecurityPolicyFactory>(), It.IsAny<MessageSecurityMode>(),
                It.IsAny<X509Certificate2>(), It.IsAny<X509Certificate2>()))
                .Throws(new Exception("Connection Failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _pool.RentAsync());

            await Assert.ThrowsAsync<Exception>(() => _pool.RentAsync());
            await Assert.ThrowsAsync<Exception>(() => _pool.RentAsync());
        }

        [Fact]
        public async Task Dispose_DisposesAllIdleClients()
        {
            // Arrange
            var c1 = await _pool.RentAsync();
            var c2 = await _pool.RentAsync();
            c1.Dispose(); // return to pool
            c2.Dispose(); // return to pool

            // Act
            _pool.Dispose();

            // Assert
            _channelMock.Verify(c => c.Dispose(), Times.AtLeastOnce());
        }

        [Fact]
        public async Task MaxSize_IsRespected()
        {
            // Our pool has max size 2
            await _pool.RentAsync();
            await _pool.RentAsync();

            // Third rent should block.
            var thirdRentTask = _pool.RentAsync();

            // Assert
            await Task.Delay(100);
            Assert.False(thirdRentTask.IsCompleted);

            _pool.Dispose();
        }
    }
}
