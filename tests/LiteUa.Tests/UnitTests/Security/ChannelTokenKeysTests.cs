using LiteUa.Security;

namespace LiteUa.Tests.UnitTests.Security
{
    [Trait("Category", "Unit")]
    public class ChannelTokenKeysTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            byte[] expectedSigningKey = [0x01, 0x02, 0x03];
            byte[] expectedEncryptingKey = [0x04, 0x05, 0x06];
            byte[] expectedIv = [0x07, 0x08, 0x09];

            // Act
            var keys = new ChannelTokenKeys(expectedSigningKey, expectedEncryptingKey, expectedIv);

            // Assert
            Assert.Equal(expectedSigningKey, keys.SigningKey);
            Assert.Equal(expectedEncryptingKey, keys.EncryptingKey);
            Assert.Equal(expectedIv, keys.InitializationVector);
        }

        [Fact]
        public void Properties_AreReadOnly()
        {
            // Arrange
            var type = typeof(ChannelTokenKeys);

            // Act & Assert
            // Verify that the properties do not have a setter
            Assert.Null(type.GetProperty(nameof(ChannelTokenKeys.SigningKey))?.SetMethod);
            Assert.Null(type.GetProperty(nameof(ChannelTokenKeys.EncryptingKey))?.SetMethod);
            Assert.Null(type.GetProperty(nameof(ChannelTokenKeys.InitializationVector))?.SetMethod);
        }

        [Fact]
        public void Constructor_AcceptsEmptyArrays()
        {
            // Act
            var keys = new ChannelTokenKeys([], [], []);

            // Assert
            Assert.Empty(keys.SigningKey);
            Assert.Empty(keys.EncryptingKey);
            Assert.Empty(keys.InitializationVector);
        }

        [Fact]
        public void Constructor_HandlesNulls()
        {
            // Act
            var keys = new ChannelTokenKeys(null!, null!, null!);

            // Assert
            Assert.Null(keys.SigningKey);
            Assert.Null(keys.EncryptingKey);
            Assert.Null(keys.InitializationVector);
        }
    }
}