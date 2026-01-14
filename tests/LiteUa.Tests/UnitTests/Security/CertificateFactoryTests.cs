using LiteUa.Security;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LiteUa.Tests.UnitTests.Security
{
    public class CertificateFactoryTests
    {
        [Fact]
        public void CreateSelfSignedCertificate_ReturnsValidCertificateWithPrivateKey()
        {
            // Arrange
            string appName = "TestApplication";
            string appUri = "urn:localhost:TestApplication";

            // Act
            using var cert = CertificateFactory.CreateSelfSignedCertificate(appName, appUri);

            // Assert
            Assert.NotNull(cert);
            Assert.True(cert.HasPrivateKey);
            Assert.Contains($"CN={appName}", cert.Subject);

            // Verify Key Strength
            using var rsa = cert.GetRSAPrivateKey();
            Assert.NotNull(rsa);
            Assert.Equal(2048, rsa.KeySize);
        }

        [Fact]
        public void CreateSelfSignedCertificate_IncludesCorrectApplicationUriInSAN()
        {
            // Arrange
            string appUri = "urn:liteua:testserver";

            // Act
            using var cert = CertificateFactory.CreateSelfSignedCertificate("Test", appUri);

            // Assert
            // The SAN extension OID is "2.5.29.17"
            var sanExtension = cert.Extensions.Cast<X509Extension>()
                                     .FirstOrDefault(e => e.Oid?.Value == "2.5.29.17");
            Assert.NotNull(sanExtension);
            string decodedSan = sanExtension.Format(true);
            Assert.Contains(appUri, decodedSan);
        }

        [Fact]
        public void CreateSelfSignedCertificate_HasRequiredOpcUaKeyUsages()
        {
            // Act
            using var cert = CertificateFactory.CreateSelfSignedCertificate("Test", "urn:test");

            // Assert Key Usage
            var keyUsage = cert.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
            Assert.NotNull(keyUsage);
            Assert.True(keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature));
            Assert.True(keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.KeyEncipherment));
            Assert.True(keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DataEncipherment));

            // Assert Enhanced Key Usage (Client and Server Auth)
            var enhancedKeyUsage = cert.Extensions.OfType<X509EnhancedKeyUsageExtension>().FirstOrDefault();
            Assert.NotNull(enhancedKeyUsage);
            var oids = enhancedKeyUsage.EnhancedKeyUsages.Cast<Oid>().Select(o => o.Value).ToList();
            Assert.Contains("1.3.6.1.5.5.7.3.1", oids); // Server Auth
            Assert.Contains("1.3.6.1.5.5.7.3.2", oids); // Client Auth
        }

        [Fact]
        public void CreateSelfSignedCertificate_IsNotValidBeforeNow()
        {
            // Act
            using var cert = CertificateFactory.CreateSelfSignedCertificate("Test", "urn:test");

            // Assert
            // Should be valid from roughly yesterday (AddDays(-1) in code)
            Assert.True(cert.NotBefore <= DateTime.UtcNow);
            // Should be valid for a long time (AddYears(25) in code)
            Assert.True(cert.NotAfter > DateTime.UtcNow.AddYears(20));
        }

        [Fact]
        public void CreateSelfSignedCertificate_InvalidUri_ThrowsException()
        {
            // Act & Assert
            // This will likely fail inside the SubjectAlternativeNameBuilder.AddUri call
            Assert.ThrowsAny<Exception>(() =>
                CertificateFactory.CreateSelfSignedCertificate("Test", "not-a-valid-uri"));
        }
    }
}
