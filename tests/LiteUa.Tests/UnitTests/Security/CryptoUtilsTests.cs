using LiteUa.Security;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LiteUa.Tests.UnitTests.Security
{
    [Trait("Category", "Unit")]
    public class CryptoUtilsTests
    {
        #region PSha256 Tests

        [Fact]
        public void PSha256_NullInputs_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CryptoUtils.PSha256(null!, [0], 10));
            Assert.Throws<ArgumentNullException>(() => CryptoUtils.PSha256([0], null!, 10));
        }

        [Theory]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(100)]
        public void PSha256_RequestedLength_ReturnsCorrectArraySize(int length)
        {
            // Arrange
            byte[] secret = System.Text.Encoding.UTF8.GetBytes("secret");
            byte[] seed = System.Text.Encoding.UTF8.GetBytes("seed");

            // Act
            byte[] result = CryptoUtils.PSha256(secret, seed, length);

            // Assert
            Assert.Equal(length, result.Length);
        }

        [Fact]
        public void PSha256_IsDeterministic()
        {
            // Arrange
            byte[] secret = [0x01, 0x02, 0x03];
            byte[] seed = [0x04, 0x05, 0x06];

            // Act
            byte[] run1 = CryptoUtils.PSha256(secret, seed, 32);
            byte[] run2 = CryptoUtils.PSha256(secret, seed, 32);

            // Assert
            Assert.Equal(run1, run2);
        }

        [Fact]
        public void PSha256_ProducesDifferentOutput_ForDifferentSeeds()
        {
            // Arrange
            byte[] secret = [0x01];
            byte[] seed1 = [0x01];
            byte[] seed2 = [0x02];

            // Act
            byte[] out1 = CryptoUtils.PSha256(secret, seed1, 32);
            byte[] out2 = CryptoUtils.PSha256(secret, seed2, 32);

            // Assert
            Assert.NotEqual(out1, out2);
        }

        [Fact]
        public void PSha256_CalculatesCorrectFirstBlock()
        {
            // Manual verification of the first HMAC iteration
            // A(1) = HMAC_SHA256(secret, seed)
            // P_hash_1 = HMAC_SHA256(secret, A(1) + seed)

            byte[] secret = System.Text.Encoding.UTF8.GetBytes("key");
            byte[] seed = System.Text.Encoding.UTF8.GetBytes("seed");

            using var hmac = new HMACSHA256(secret);
            byte[] a1 = hmac.ComputeHash(seed);
            byte[] block1Input = new byte[a1.Length + seed.Length];
            Buffer.BlockCopy(a1, 0, block1Input, 0, a1.Length);
            Buffer.BlockCopy(seed, 0, block1Input, a1.Length, seed.Length);
            byte[] expectedFirstBlock = hmac.ComputeHash(block1Input);

            // Act
            byte[] result = CryptoUtils.PSha256(secret, seed, 32);

            // Assert
            // First 32 bytes of result should match the manual calculation
            byte[] actualFirstBlock = new byte[32];
            Buffer.BlockCopy(result, 0, actualFirstBlock, 0, 32);

            Assert.Equal(expectedFirstBlock, actualFirstBlock);
        }

        #endregion

        #region ParseAlgorithm Tests

        [Theory]
        [InlineData("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256", "SHA256")]
        [InlineData("http://www.w3.org/2000/09/xmldsig#rsa-sha1", "SHA1")]
        public void ParseAlgorithm_ValidUri_ReturnsCorrectParameters(string uri, string expectedHashName)
        {
            // Act
            var (hashName, padding) = CryptoUtils.ParseAlgorithm(uri);

            // Assert
            Assert.Equal(expectedHashName, hashName.Name);
            Assert.Equal(RSASignaturePadding.Pkcs1, padding);
        }

        [Fact]
        public void ParseAlgorithm_UnsupportedUri_ThrowsNotSupportedException()
        {
            // Act & Assert
            Assert.Throws<NotSupportedException>(() => CryptoUtils.ParseAlgorithm("http://invalid.uri"));
        }

        #endregion
    }
}
