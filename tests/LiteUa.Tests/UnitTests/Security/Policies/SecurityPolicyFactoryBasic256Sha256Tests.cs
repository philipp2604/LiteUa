using LiteUa.Security.Policies;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Tests.UnitTests.Security.Policies
{
    [Trait("Category", "Unit")]
    public class SecurityPolicyFactoryBasic256Sha256Tests : IDisposable
    {
        private readonly SecurityPolicyFactoryBasic256Sha256 _factory;
        private readonly X509Certificate2 _testCert;

        public SecurityPolicyFactoryBasic256Sha256Tests()
        {
            _factory = new SecurityPolicyFactoryBasic256Sha256();
            // Create one valid certificate to use for various parameters
            _testCert = CreateSelfSignedCertificate("TestCert");
        }

        [Fact]
        public void CreateSecurityPolicy_ValidCerts_ReturnsBasic256Sha256Policy()
        {
            // Act
            var result = _factory.CreateSecurityPolicy(_testCert, _testCert);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<SecurityPolicyBasic256Sha256>(result);
            Assert.Equal(SecurityPolicyUris.Basic256Sha256, result.SecurityPolicyUri);
        }

        [Fact]
        public void CreateSecurityPolicy_LocalCertNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _factory.CreateSecurityPolicy(null, _testCert));
        }

        [Fact]
        public void CreateSecurityPolicy_RemoteCertNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _factory.CreateSecurityPolicy(_testCert, null));
        }

        [Fact]
        public void CreateSecurityPolicy_CertWithoutPrivateKey_ThrowsException()
        {
            // Arrange
            // Create a public-only version of the certificate
            var publicOnlyCert = X509CertificateLoader.LoadCertificate(_testCert.Export(X509ContentType.Cert));

            // Act & Assert
            Assert.ThrowsAny<Exception>(() =>
                _factory.CreateSecurityPolicy(publicOnlyCert, _testCert));
        }

        private static X509Certificate2 CreateSelfSignedCertificate(string name)
        {
            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                $"CN={name}, DC={Dns.GetHostName()}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));

            using var certEphemeral = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(7));

            string certPem = certEphemeral.ExportCertificatePem();
            string keyPem = rsa.ExportPkcs8PrivateKeyPem();
            return X509Certificate2.CreateFromPem(certPem, keyPem);
        }

        public void Dispose()
        {
            _testCert.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}