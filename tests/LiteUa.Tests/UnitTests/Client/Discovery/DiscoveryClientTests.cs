using LiteUa.Client.Discovery;
using LiteUa.Security.Policies;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using Moq;

namespace LiteUa.Tests.UnitTests.Client.Discovery
{
    [Trait("Category", "Unit")]
    public class DiscoveryClientTests
    {
        private readonly Mock<IUaTcpClientChannelFactory> _factoryMock;
        private readonly Mock<IUaTcpClientChannel> _channelMock;
        private readonly DiscoveryClient _sut;

        private const string TestUrl = "opc.tcp://localhost:4840";
        private const string AppUri = "urn:test:client";
        private const string ProdUri = "urn:test:prod";
        private const string AppName = "TestClient";

        public DiscoveryClientTests()
        {
            _factoryMock = new Mock<IUaTcpClientChannelFactory>();
            _channelMock = new Mock<IUaTcpClientChannel>();

            _factoryMock.Setup(f => f.CreateTcpClientChannel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                                           It.IsAny<ISecurityPolicyFactory>(), It.IsAny<MessageSecurityMode>(), null, null, It.IsAny<uint>(), It.IsAny<uint>()))
                        .Returns(_channelMock.Object);

            _sut = new DiscoveryClient(TestUrl, AppUri, ProdUri, AppName, 20000, 10000, _factoryMock.Object);
        }

        [Fact]
        public async Task GetEndpoint_Success_ReturnsMatchingEndpoint()
        {
            // Arrange
            var targetPolicy = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256";
            var expectedEndpoint = new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = targetPolicy,
                UserIdentityTokens = [new UserTokenPolicy { TokenType = (int)UserTokenType.Username }]
            };

            _channelMock.Setup(c => c.GetEndpointsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEndpointsResponse { Endpoints = [expectedEndpoint] });

            // Act
            var result = await _sut.GetEndpoint(MessageSecurityMode.SignAndEncrypt, targetPolicy, UserTokenType.Username);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetPolicy, result.SecurityPolicyUri);
            _channelMock.Verify(c => c.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetEndpoint_NoMatch_ReturnsNull()
        {
            // Arrange
            var serverEndpoints = new[]
            {
            new EndpointDescription { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = "None" }
        };

            _channelMock.Setup(c => c.GetEndpointsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEndpointsResponse { Endpoints = serverEndpoints });

            // Act
            // Requesting SignAndEncrypt when server only offers None
            var result = await _sut.GetEndpoint(MessageSecurityMode.SignAndEncrypt, "SomePolicy", UserTokenType.Anonymous);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetEndpoint_EmptyServerResponse_ReturnsNull()
        {
            // Arrange: Server returns an empty list
            _channelMock.Setup(c => c.GetEndpointsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEndpointsResponse { Endpoints = [] });

            // Act
            var result = await _sut.GetEndpoint(MessageSecurityMode.None, "None", UserTokenType.Anonymous);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetEndpoint_NullUserTokensOnServer_HandlesGracefully()
        {
            // Arrange: Server returns endpoint but UserIdentityTokens is null
            var endpointWithNullTokens = new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = "None",
                UserIdentityTokens = null // Edge Case
            };

            _channelMock.Setup(c => c.GetEndpointsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEndpointsResponse { Endpoints = [endpointWithNullTokens] });

            // Act
            var result = await _sut.GetEndpoint(MessageSecurityMode.None, "None", UserTokenType.Anonymous);

            // Assert
            Assert.Null(result); // Should be null because filtering requires a matching token type
        }

        [Fact]
        public async Task GetEndpoint_PartialMatch_SecurityModeMismatch()
        {
            // Arrange: Policy matches, but Mode does not
            var serverEndpoints = new[]
            {
            new EndpointDescription { SecurityMode = MessageSecurityMode.Sign, SecurityPolicyUri = "PolicyA" }
        };

            _channelMock.Setup(c => c.GetEndpointsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEndpointsResponse { Endpoints = serverEndpoints });

            // Act
            var result = await _sut.GetEndpoint(MessageSecurityMode.SignAndEncrypt, "PolicyA", UserTokenType.Anonymous);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetEndpoint_PassesCancellationTokenToChannel()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            _channelMock.Setup(c => c.GetEndpointsAsync(cts.Token))
                .ReturnsAsync(new GetEndpointsResponse());

            // Act
            await _sut.GetEndpoint(MessageSecurityMode.None, "None", UserTokenType.Anonymous, cts.Token);

            // Assert
            _channelMock.Verify(c => c.ConnectAsync(cts.Token), Times.Once);
            _channelMock.Verify(c => c.GetEndpointsAsync(cts.Token), Times.Once);
        }

        [Fact]
        public async Task GetEndpoint_EnsuresChannelIsDisposed()
        {
            // Arrange
            _channelMock.Setup(c => c.GetEndpointsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEndpointsResponse());

            // Act
            await _sut.GetEndpoint(MessageSecurityMode.None, "None", UserTokenType.Anonymous);

            // Assert
            _channelMock.Verify(c => c.DisposeAsync(), Times.Once);
        }
    }
}