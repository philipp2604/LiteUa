using LiteUa.Encoding;
using LiteUa.Stack.Session.Identity;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Session.Identity
{
    [Trait("Category", "Unit")]
    public class UserNameIdentityTokenTests : IDisposable
    {
        private readonly X509Certificate2 _serverCert;

        public UserNameIdentityTokenTests()
        {
            _serverCert = CreateSelfSignedCertificate("OpcServer");
        }

        [Fact]
        public void Constructor_SetsProperties()
        {
            var token = new UserNameIdentityToken("p1", "u1", "pw1");
            Assert.Equal("p1", token.PolicyId);
            Assert.Equal("u1", token.UserName);
            Assert.Equal("pw1", token.Password);
        }

        [Fact]
        public void ToExtensionObject_NullArguments_ThrowsException()
        {
            var token = new UserNameIdentityToken("id", "user", "pass");

            Assert.Throws<ArgumentNullException>(() => token.ToExtensionObject(null, [1]));
            Assert.Throws<ArgumentNullException>(() => token.ToExtensionObject(_serverCert, null));
            Assert.Throws<ArgumentNullException>(() => token.ToExtensionObject(_serverCert, []));
        }

        [Fact]
        public void ToExtensionObject_Metadata_IsCorrect()
        {
            // Arrange
            var token = new UserNameIdentityToken("Policy1", "User1", "Secret");
            byte[] nonce = new byte[32];
            RandomNumberGenerator.Fill(nonce);

            // Act
            var ext = token.ToExtensionObject(_serverCert, nonce);

            // Assert
            // 324: numeric identifier for UserNameIdentityToken
            Assert.Equal(324u, ext.TypeId.NumericIdentifier);
            Assert.Equal(0x01, ext.Encoding); // Binary
        }

        [Fact]
        public void ToExtensionObject_EncryptionBlock_IsSpecCompliant()
        {
            // Arrange
            string password = "MySecretPassword";
            byte[] nonce = new byte[32];
            RandomNumberGenerator.Fill(nonce);
            var token = new UserNameIdentityToken("PolicyId", "UserName", password);

            // Act
            var ext = token.ToExtensionObject(_serverCert, nonce);

            // Assert:
            using var ms = new MemoryStream(ext.Body!);
            var reader = new OpcUaBinaryReader(ms);

            string policyId = reader.ReadString()!;
            string userName = reader.ReadString()!;
            byte[] encryptedBlock = reader.ReadByteString()!;
            string algoUri = reader.ReadString()!;

            Assert.Equal("PolicyId", policyId);
            Assert.Equal("UserName", userName);
            Assert.Equal("http://www.w3.org/2001/04/xmlenc#rsa-oaep", algoUri);

            // Act: 
            using var rsa = _serverCert.GetRSAPrivateKey();
            byte[] decrypted = rsa!.Decrypt(encryptedBlock, RSAEncryptionPadding.OaepSHA1);

            // Decrypted block must be [Length(4)] [Password] [Nonce]
            int lengthPrefix = BinaryPrimitives.ReadInt32LittleEndian(decrypted.AsSpan(0, 4));

            byte[] pwBytes = System.Text.Encoding.UTF8.GetBytes(password);
            Assert.Equal(pwBytes.Length + nonce.Length, lengthPrefix);

            string decryptedPw = System.Text.Encoding.UTF8.GetString(decrypted, 4, pwBytes.Length);
            Assert.Equal(password, decryptedPw);

            byte[] decryptedNonce = decrypted.AsSpan(4 + pwBytes.Length).ToArray();
            Assert.Equal(nonce, decryptedNonce);
        }

        private static X509Certificate2 CreateSelfSignedCertificate(string name)
        {
            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                $"CN={name}, DC={Dns.GetHostName()}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));

            using var certEphemeral = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(1));

            return X509Certificate2.CreateFromPem(certEphemeral.ExportCertificatePem(), rsa.ExportPkcs8PrivateKeyPem());
        }

        public void Dispose()
        {
            _serverCert.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
