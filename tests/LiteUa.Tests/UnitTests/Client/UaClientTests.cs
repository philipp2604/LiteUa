using LiteUa.BuiltIn;
using LiteUa.Client;
using LiteUa.Client.Building;
using LiteUa.Client.Discovery;
using LiteUa.Client.Pooling;
using LiteUa.Client.Subscriptions;
using LiteUa.Security.Policies;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Client
{
    [Trait("Category", "Unit")]
    public class UaClientTests
    {
        private readonly Mock<IUaTcpClientChannelFactory> _tcpFactoryMock;
        private readonly Mock<IUaInnerClientsFactory> _innerFactoryMock;
        private readonly Mock<IDiscoveryClient> _discoveryMock;
        private readonly Mock<ISubscriptionClient> _subClientMock;
        private readonly Mock<IUaClientPool> _poolMock;
        private readonly Mock<IUaTcpClientChannel> _channelMock;

        private readonly UaClientOptions _options;
        private readonly UaClient _sut;

        public UaClientTests()
        {
            _tcpFactoryMock = new Mock<IUaTcpClientChannelFactory>();
            _innerFactoryMock = new Mock<IUaInnerClientsFactory>();

            _discoveryMock = new Mock<IDiscoveryClient>();
            _subClientMock = new Mock<ISubscriptionClient>();
            _poolMock = new Mock<IUaClientPool>();
            _channelMock = new Mock<IUaTcpClientChannel>();

            _options = new UaClientOptions
            {
                EndpointUrl = "opc.tcp://localhost:4840",
                Security = { UserTokenType = UserTokenType.Anonymous }
            };

            _innerFactoryMock.Setup(f => f.CreateDiscoveryClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<IUaTcpClientChannelFactory>()))
                .Returns(_discoveryMock.Object);

            _innerFactoryMock.Setup(f => f.CreateSubscriptionClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IUserIdentity>(), It.IsAny<ISecurityPolicyFactory>(), It.IsAny<MessageSecurityMode>(), It.IsAny<System.Security.Cryptography.X509Certificates.X509Certificate2>(), It.IsAny<System.Security.Cryptography.X509Certificates.X509Certificate2>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<double>(), It.IsAny<uint>(), It.IsAny<IUaTcpClientChannelFactory>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(_subClientMock.Object);

            _innerFactoryMock.Setup(f => f.CreateUaClientPool(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IUserIdentity>(), It.IsAny<ISecurityPolicyFactory>(), It.IsAny<MessageSecurityMode>(), It.IsAny<System.Security.Cryptography.X509Certificates.X509Certificate2>(), It.IsAny<System.Security.Cryptography.X509Certificates.X509Certificate2>(), It.IsAny<int>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<IUaTcpClientChannelFactory>()))
                .Returns(_poolMock.Object);

            _sut = new UaClient(_options, _tcpFactoryMock.Object, _innerFactoryMock.Object);
        }

        [Fact]
        public async Task ConnectAsync_OrchestratesDiscoveryAndClientSetup()
        {
            // Arrange
            var endpoint = new EndpointDescription
            {
                UserIdentityTokens = [new UserTokenPolicy { TokenType = (int)UserTokenType.Anonymous }]
            };
            _discoveryMock.Setup(d => d.GetEndpoint(It.IsAny<MessageSecurityMode>(), It.IsAny<string>(), It.IsAny<UserTokenType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(endpoint);

            // Act
            await _sut.ConnectAsync();

            // Assert
            _discoveryMock.Verify(d => d.GetEndpoint(It.IsAny<MessageSecurityMode>(), It.IsAny<string>(), It.IsAny<UserTokenType>(), It.IsAny<CancellationToken>()), Times.Once);
            _subClientMock.Verify(s => s.Start(), Times.Once);
            _innerFactoryMock.Verify(f => f.CreateUaClientPool(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IUserIdentity>(), It.IsAny<ISecurityPolicyFactory>(), It.IsAny<MessageSecurityMode>(), It.IsAny<System.Security.Cryptography.X509Certificates.X509Certificate2>(), It.IsAny<System.Security.Cryptography.X509Certificates.X509Certificate2>(), It.IsAny<int>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<IUaTcpClientChannelFactory>()), Times.Once);
        }

        [Fact]
        public async Task ReadNodesAsync_RentsFromPool_AndMarksInvalidOnException()
        {
            // Arrange
            // Manually set internal pool
            _sut._pool = _poolMock.Object;

            var pooledClient = new PooledUaClient(_channelMock.Object, Mock.Of<IUaClientPool>());
            _poolMock.Setup(p => p.RentAsync()).ReturnsAsync(pooledClient);

            _channelMock.Setup(c => c.ReadAsync(It.IsAny<NodeId[]>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Network Error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.ReadNodesAsync([new NodeId(10)]));

            Assert.True(pooledClient.IsInvalid);
            _poolMock.Verify(p => p.RentAsync(), Times.Once);
        }

        [Fact]
        public async Task SubscribeAsync_MapsHandlesToCallbacks()
        {
            // Arrange
            _sut._subscriptionClient = _subClientMock.Object;
            var nodeIds = new[] { new NodeId(1) };
            var handles = new uint[] { 50 };
            _subClientMock.Setup(s => s.SubscribeAsync(nodeIds, It.IsAny<double>())).ReturnsAsync(handles);

            bool callbackCalled = false;
            void myCallback(uint h, DataValue v) => callbackCalled = true;

            // Act
            await _sut.SubscribeAsync(nodeIds, myCallback);

            // Simulate event from internal SubscriptionClient
            var privateMethod = typeof(UaClient).GetMethod("OnSubscriptionDataChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            privateMethod?.Invoke(_sut, [50u, new DataValue()]);

            // Assert
            Assert.True(callbackCalled);
        }

        [Fact]
        public async Task WriteNodesAsync_ThrowsException_IfStatusIsBad()
        {
            // Arrange
            _sut._pool = _poolMock.Object;
            var pooledClient = new PooledUaClient(_channelMock.Object, Mock.Of<IUaClientPool>());
            _poolMock.Setup(p => p.RentAsync()).ReturnsAsync(pooledClient);

            // Mock a "Bad" status code result
            var badResult = new[] { new StatusCode(0x80010000) }; // Bad_UnexpectedError
            _channelMock.Setup(c => c.WriteAsync(It.IsAny<NodeId[]>(), It.IsAny<DataValue[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(badResult);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _sut.WriteNodesAsync([new NodeId(1)], [new DataValue()]));
            Assert.Contains("Write failed", ex.Message);
        }

        [Fact]
        public async Task EnsureConnected_Throws_WhenNotConnected()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ReadNodesAsync([new NodeId(1)]));
        }

        [Fact]
        public async Task DisposeAsync_CleansUpAllResources()
        {
            // Arrange
            _sut._pool = _poolMock.Object;
            _sut._subscriptionClient = _subClientMock.Object;

            // Act
            await _sut.DisposeAsync();

            // Assert
            _poolMock.Verify(p => p.DisposeAsync(), Times.Once);
            _subClientMock.Verify(s => s.DisposeAsync(), Times.Once);
        }
    }
}
