using LiteUa.Encoding;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.SecureChannel;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Discovery
{
    [Trait("Category", "Unit")]
    public class EndpointDescriptionTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public EndpointDescriptionTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_FullDescription_ParsesCorrectly()
        {
            // Arrange
            // 1. Strings Sequence (Global)
            // Order: EndpointUrl -> (AppDesc: AppUri, ProdUri, AppNameText, AppNameLocale, Gateway, Profile) -> SecurityPolicyUri -> TransportProfileUri
            _readerMock.SetupSequence(r => r.ReadString())
                .Returns("opc.tcp://localhost:4840") // EndpointUrl
                .Returns("urn:app")                  // AppDesc: ApplicationUri
                .Returns("urn:prod")                 // AppDesc: ProductUri
                .Returns("AppName")                  // AppDesc: LocalizedText.Text
                .Returns("AppLocale")                // AppDesc: LocalizedText.Locale
                .Returns("gw")                       // AppDesc: Gateway
                .Returns("profile")                  // AppDesc: DiscoveryProfile
                .Returns("http://security-policy")   // SecurityPolicyUri
                .Returns("http://transport-profile");// TransportProfileUri

            // 2. Int32 Sequence (Global)
            // Order: (AppDesc: Type, DiscoveryUrlsCount) -> SecurityMode -> UserTokenPolicyCount
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)  // AppDesc: Type
                .Returns(0)  // AppDesc: DiscoveryUrls (Empty)
                .Returns(3)  // SecurityMode (SignAndEncrypt)
                .Returns(0); // UserTokenPolicy (Empty)

            // 3. Bytes and ByteStrings
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns((byte)0x03)    // LocalizedText Mask (Both) inside ApplicationDescription
                .Returns((byte)50);     // SecurityLevel (Final field)

            _readerMock.Setup(r => r.ReadByteString()).Returns([0x01, 0x02, 0x03]); // ServerCertificate

            // Act
            var result = EndpointDescription.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("opc.tcp://localhost:4840", result.EndpointUrl);
            Assert.NotNull(result.Server);
            Assert.Equal("urn:app", result.Server.ApplicationUri);
            Assert.Equal([0x01, 0x02, 0x03], result.ServerCertificate);
            Assert.Equal(MessageSecurityMode.SignAndEncrypt, result.SecurityMode);
            Assert.Equal("http://security-policy", result.SecurityPolicyUri);
            Assert.Equal("http://transport-profile", result.TransportProfileUri);
            Assert.Equal(50, result.SecurityLevel);
        }

        [Fact]
        public void Decode_WithUserTokenPolicies_ParsesArray()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadString()).Returns("test");
            _readerMock.Setup(r => r.ReadByte()).Returns(0); // For LocalizedText Mask and SecurityLevel
            _readerMock.Setup(r => r.ReadByteString()).Returns([]);

            // Sequence for Int32:
            // 1. AppType, 2. DiscoveryUrlCount, 3. SecurityMode, 4. UserTokenPolicyCount (1)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0).Returns(0).Returns(1).Returns(1);

            // Act
            var result = EndpointDescription.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result.UserIdentityTokens);
            Assert.Single(result.UserIdentityTokens);
        }

        [Fact]
        public void Decode_EmptyTokenPolicies_ReturnsEmptyArray()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadString()).Returns("test");
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Count = 0
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0).Returns(0).Returns(1).Returns(0);

            // Act
            var result = EndpointDescription.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result.UserIdentityTokens);
            Assert.Empty(result.UserIdentityTokens);
        }

        [Fact]
        public void Decode_SequenceCheck_VerifiesFinalFields()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadString()).Returns(() =>
            {
                callOrder.Add("String");
                return "some-string";
            });
            _readerMock.Setup(r => r.ReadByte()).Returns(() =>
            {
                callOrder.Add("Byte");
                return 0;
            });
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadByteString()).Returns([]);

            // Act
            EndpointDescription.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("Byte", callOrder.Last());
            Assert.Equal("String", callOrder[^2]);
            var lastTwo = callOrder.Skip(callOrder.Count - 2).ToList();
            Assert.Equal("String", lastTwo[0]);
            Assert.Equal("Byte", lastTwo[1]);
        }
    }
}