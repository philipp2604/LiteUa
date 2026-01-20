using LiteUa.Encoding;
using LiteUa.Stack.SecureChannel;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.SecureChannel
{
    [Trait("Category", "Unit")]
    public class OpenSecureChannelResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public OpenSecureChannelResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(449u, OpenSecureChannelResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_AllFieldsPresent_ParsesCorrectly()
        {
            // Arrange
            var expectedNonce = new byte[] { 0x1, 0x2, 0x3, 0x4 };
            var expectedCreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            _readerMock.Setup(r => r.ReadInt64()).Returns(0); // Header Timestamp
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)    // Header Handle
                .Returns(0u)    // Header ServiceResult
                .Returns(1u);   // ServerProtocolVersion (The first field after Header)

            _readerMock.Setup(r => r.ReadByte()).Returns(0); // Header Diags / NodeId types
            _readerMock.Setup(r => r.ReadInt32()).Returns(0); // Header StringTable Count

            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)    // Header Handle
                .Returns(0u)    // Header ServiceResult
                .Returns(1u)    // ServerProtocolVersion
                .Returns(555u)  // ChannelId
                .Returns(999u)  // TokenId
                .Returns(3600u); // RevisedLifetime

            _readerMock.SetupSequence(r => r.ReadDateTime())
                .Returns(DateTime.MinValue) // Header Timestamp (ResponseHeader.Decode)
                .Returns(expectedCreatedAt); // Token CreatedAt (ChannelSecurityToken.Decode)

            _readerMock.Setup(r => r.ReadByteString()).Returns(expectedNonce);

            // Act
            var response = new OpenSecureChannelResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ResponseHeader);
            Assert.Equal(1u, response.ServerProtocolVersion);
            Assert.NotNull(response.SecurityToken);
            Assert.Equal(555u, response.SecurityToken.ChannelId);
            Assert.Equal(999u, response.SecurityToken.TokenId);
            Assert.Equal(expectedCreatedAt, response.SecurityToken.CreatedAt);
            Assert.Equal(expectedNonce, response.ServerNonce);
        }

        [Fact]
        public void Decode_NullNonce_SetsPropertyToNull()
        {
            // Arrange
            // Return null for the nonce
            _readerMock.Setup(r => r.ReadByteString()).Returns((byte[]?)null);

            // Act
            var response = new OpenSecureChannelResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Null(response.ServerNonce);
        }

        [Fact]
        public void Decode_SequenceCheck_VerifiesFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() => {
                callOrder.Add("UInt32");
                return 0;
            });
            _readerMock.Setup(r => r.ReadByteString()).Returns(() => {
                callOrder.Add("Nonce");
                return null;
            });

            // Act
            var response = new OpenSecureChannelResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("Nonce", callOrder[^1]);
            Assert.True(callOrder.Count(x => x == "UInt32") >= 4);
        }
    }
}
