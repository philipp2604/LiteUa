using LiteUa.Encoding;
using LiteUa.Stack.Session.Identity;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Session.Identity
{
    [Trait("Category", "Unit")]
    public class AnonymousIdentityTokenTests
    {
        [Fact]
        public void Constructor_SetsDefaultPolicyId()
        {
            // Act
            var token = new AnonymousIdentityToken();

            // Assert
            Assert.Equal("Anonymous", token.PolicyId);
        }

        [Fact]
        public void Constructor_SetsCustomPolicyId()
        {
            // Act
            var token = new AnonymousIdentityToken("MyCustomPolicy");

            // Assert
            Assert.Equal("MyCustomPolicy", token.PolicyId);
        }

        [Fact]
        public void ToExtensionObject_ReturnsCorrectMetadata()
        {
            // Arrange
            var token = new AnonymousIdentityToken();

            // Act
            var extObj = token.ToExtensionObject(null, null);

            // Assert
            // 321. numeric identifier for AnonymousIdentityToken
            Assert.Equal(321u, extObj.TypeId.NumericIdentifier);
            // Encoding 0x01: ByteString/Binary body
            Assert.Equal(0x01, extObj.Encoding);
        }

        [Fact]
        public void ToExtensionObject_EncodesPolicyIdInBody()
        {
            // Arrange
            string expectedPolicy = "SecureAnonymous";
            var token = new AnonymousIdentityToken(expectedPolicy);

            // Act
            var extObj = token.ToExtensionObject(null, null);

            // Assert:
            Assert.NotNull(extObj.Body);
            using var ms = new MemoryStream(extObj.Body);
            var reader = new OpcUaBinaryReader(ms);

            string actualPolicy = reader.ReadString()!;
            Assert.Equal(expectedPolicy, actualPolicy);
        }

        [Fact]
        public void ToExtensionObject_IgnoresCertificateAndNonce()
        {
            // Arrange
            var token = new AnonymousIdentityToken();
            X509Certificate2? dummyCert = null;
            var dummyNonce = new byte[] { 1, 2, 3 };

            // Act
            var extObj = token.ToExtensionObject(dummyCert, dummyNonce);

            // Assert
            Assert.NotNull(extObj);
            Assert.Equal(321u, extObj.TypeId.NumericIdentifier);
        }
    }
}
