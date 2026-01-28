using LiteUa.Client;
using LiteUa.Client.Discovery;
using LiteUa.Client.Pooling;
using LiteUa.Client.Subscriptions;
using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using Moq;

namespace LiteUa.Tests.UnitTests.Client
{
    [Trait("Category", "Unit")]
    public class UaInnerClientsFactoryTests
    {
        private readonly UaInnerClientsFactory _factory;
        private readonly Mock<IUserIdentity> _userIdentityMock;
        private readonly Mock<ISecurityPolicyFactory> _policyFactoryMock;
        private readonly Mock<IUaTcpClientChannelFactory> _channelFactoryMock;

        public UaInnerClientsFactoryTests()
        {
            _factory = new UaInnerClientsFactory();
            _userIdentityMock = new Mock<IUserIdentity>();
            _policyFactoryMock = new Mock<ISecurityPolicyFactory>();
            _channelFactoryMock = new Mock<IUaTcpClientChannelFactory>();
        }

        [Fact]
        public void CreateSubscriptionClient_ReturnsCorrectType()
        {
            // Act
            var result = _factory.CreateSubscriptionClient(
                "opc.tcp://localhost:4840", "uri", "prod", "app",
                _userIdentityMock.Object, _policyFactoryMock.Object,
                MessageSecurityMode.None, null, null, 20000, 10000, 3, 2.0, 10000, _channelFactoryMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<SubscriptionClient>(result);
        }

        [Fact]
        public void CreateDiscoveryClient_ReturnsCorrectType()
        {
            // Act
            var result = _factory.CreateDiscoveryClient(
                "opc.tcp://localhost:4840", "uri", "prod", "app", 20000, 10000,
                _channelFactoryMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DiscoveryClient>(result);
        }

        [Fact]
        public void CreateUaClientPool_ReturnsCorrectType()
        {
            // Act
            var result = _factory.CreateUaClientPool(
                "opc.tcp://localhost:4840", "uri", "prod", "app",
                _userIdentityMock.Object, _policyFactoryMock.Object,
                MessageSecurityMode.None, null, null, 10, 20000, 10000, _channelFactoryMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<UaClientPool>(result);
        }

        [Fact]
        public void CreateDiscoveryClient_InvalidUrl_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _factory.CreateDiscoveryClient("", "uri", "prod", "app", 20000, 10000, _channelFactoryMock.Object));
        }

        [Fact]
        public void CreateUaClientPool_NullIdentity_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _factory.CreateUaClientPool("url", "uri", "prod", "app",
                null!, _policyFactoryMock.Object, MessageSecurityMode.None, null, null, 1, 20000, 10000, _channelFactoryMock.Object));
        }
    }
}