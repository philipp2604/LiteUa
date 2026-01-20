using LiteUa.Security.Policies;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Security.Policies
{
    [Trait("Category", "Unit")]
    public class SecurityPolicyNoneTests
    {
        private readonly SecurityPolicyNone _policy;

        public SecurityPolicyNoneTests()
        {
            _policy = new SecurityPolicyNone();
        }

        [Fact]
        public void Configuration_Properties_AreCorrectForNone()
        {
            // Assert Metadata
            Assert.Equal(SecurityPolicyUris.None, _policy.SecurityPolicyUri);
            Assert.Equal(0, _policy.AsymmetricSignatureSize);
            Assert.Equal(1, _policy.AsymmetricEncryptionBlockSize);
            Assert.Equal(1, _policy.AsymmetricCipherTextBlockSize);
            Assert.Equal(0, _policy.SymmetricSignatureSize);
            Assert.Equal(1, _policy.SymmetricBlockSize);
            Assert.Equal(0, _policy.SymmetricKeySize);
            Assert.Equal(0, _policy.SymmetricInitializationVectorSize);
        }

        [Fact]
        public void AsymmetricOperations_PassThroughData()
        {
            // Arrange
            byte[] originalData = System.Text.Encoding.UTF8.GetBytes("PlainText Data");

            // Act & Assert
            Assert.Empty(_policy.Sign(originalData));
            Assert.True(_policy.Verify(originalData, [1, 2, 3])); // Always true
            Assert.Equal(originalData, _policy.EncryptAsymmetric(originalData));
            Assert.Equal(originalData, _policy.DecryptAsymmetric(originalData));
        }

        [Fact]
        public void SymmetricOperations_PassThroughData()
        {
            // Arrange
            byte[] originalData = System.Text.Encoding.UTF8.GetBytes("Symmetric Data");

            // Act
            _policy.DeriveKeys([], []); // Should do nothing

            // Assert
            Assert.Empty(_policy.SignSymmetric(originalData));
            Assert.True(_policy.VerifySymmetric(originalData, [0xFF]));
            Assert.Equal(originalData, _policy.EncryptSymmetric(originalData));
            Assert.Equal(originalData, _policy.DecryptSymmetric(originalData));
        }

        [Fact]
        public void DeriveKeys_DoesNotThrow()
        {
            // Act & Assert (No crashes)
            _policy.DeriveKeys(null!, null!);
            _policy.DeriveKeys([], []);
        }
    }
}
