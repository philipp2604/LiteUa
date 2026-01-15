using LiteUa.Encoding;
using LiteUa.Stack.Session.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Session.Identity
{
    [Trait("Category", "Unit")]
    public class UserTokenPolicyTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public UserTokenPolicyTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidData_SetsAllProperties()
        {
            // Arrange
            // Sequence: PolicyId -> TokenType -> IssuedTokenType -> IssuerEndpointUrl -> SecurityPolicyUri
            _readerMock.SetupSequence(r => r.ReadString())
                .Returns("UsernamePolicy")     // PolicyId
                .Returns("IssuedType")         // IssuedTokenType
                .Returns("http://issuer.com")  // IssuerEndpointUrl
                .Returns("http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256"); // SecurityPolicyUri

            _readerMock.Setup(r => r.ReadInt32()).Returns(1); // TokenType (Username = 1)

            // Act
            var result = UserTokenPolicy.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("UsernamePolicy", result.PolicyId);
            Assert.Equal(1, result.TokenType);
            Assert.Equal("IssuedType", result.IssuedTokenType);
            Assert.Equal("http://issuer.com", result.IssuerEndpointUrl);
            Assert.Equal("http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256", result.SecurityPolicyUri);
        }

        [Fact]
        public void Decode_NullStrings_SetsPropertiesToNull()
        {
            // Arrange
            // null strings (Binary length -1)
            _readerMock.Setup(r => r.ReadString()).Returns((string?)null);
            _readerMock.Setup(r => r.ReadInt32()).Returns(0); // Anonymous

            // Act
            var result = UserTokenPolicy.Decode(_readerMock.Object);

            // Assert
            Assert.Null(result.PolicyId);
            Assert.Null(result.IssuedTokenType);
            Assert.Null(result.IssuerEndpointUrl);
            Assert.Null(result.SecurityPolicyUri);
            Assert.Equal(0, result.TokenType);
        }

        [Fact]
        public void Decode_VerifiesFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadString()).Returns(() =>
            {
                callOrder.Add("ReadString");
                return "s";
            });

            _readerMock.Setup(r => r.ReadInt32()).Returns(() =>
            {
                callOrder.Add("ReadInt32");
                return 0;
            });

            // Act
            UserTokenPolicy.Decode(_readerMock.Object);

            // Assert
            // Order: String (PolicyId) -> Int32 (TokenType) -> Strings (Rest)
            Assert.Equal("ReadString", callOrder[0]);
            Assert.Equal("ReadInt32", callOrder[1]);
            Assert.Equal("ReadString", callOrder[2]);
            Assert.Equal("ReadString", callOrder[3]);
            Assert.Equal("ReadString", callOrder[4]);

            Assert.Equal(5, callOrder.Count);
        }
    }
}
