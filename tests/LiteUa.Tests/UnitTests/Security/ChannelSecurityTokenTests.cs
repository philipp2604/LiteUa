using LiteUa.Encoding;
using LiteUa.Security;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Security
{
    [Trait("Category", "Unit")]
    public class ChannelSecurityTokenTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public ChannelSecurityTokenTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidStream_ReturnsPopulatedToken()
        {
            // Arrange
            uint expectedChannelId = 12345;
            uint expectedTokenId = 1;
            uint expectedLifetime = 3600000;
            DateTime expectedCreatedAt = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(expectedChannelId)
                .Returns(expectedTokenId)
                .Returns(expectedLifetime);

            _readerMock.Setup(r => r.ReadDateTime()).Returns(expectedCreatedAt);

            // Act
            var result = ChannelSecurityToken.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedChannelId, result.ChannelId);
            Assert.Equal(expectedTokenId, result.TokenId);
            Assert.Equal(expectedCreatedAt, result.CreatedAt);
            Assert.Equal(expectedLifetime, result.RevisedLifetime);

            // Verify
            _readerMock.Verify(r => r.ReadUInt32(), Times.Exactly(3));
            _readerMock.Verify(r => r.ReadDateTime(), Times.Once);
        }

        [Fact]
        public void Properties_AreMutable()
        {
            // Arrange
            var token = new ChannelSecurityToken();
            var now = DateTime.UtcNow;

            // Act
            token.ChannelId = 10;
            token.TokenId = 20;
            token.CreatedAt = now;
            token.RevisedLifetime = 300;

            // Assert
            Assert.Equal(10u, token.ChannelId);
            Assert.Equal(20u, token.TokenId);
            Assert.Equal(now, token.CreatedAt);
            Assert.Equal(300u, token.RevisedLifetime);
        }

        [Fact]
        public void Decode_TruncatedStream_PropagatesException()
        {
            // Arrange
            // Simulate a failure on the second UInt32 read (TokenId)
            _readerMock.Setup(r => r.ReadUInt32()).Returns(1u);
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(1u)
                .Throws(new EndOfStreamException());

            // Act & Assert
            Assert.Throws<EndOfStreamException>(() => ChannelSecurityToken.Decode(_readerMock.Object));
        }
    }
}
