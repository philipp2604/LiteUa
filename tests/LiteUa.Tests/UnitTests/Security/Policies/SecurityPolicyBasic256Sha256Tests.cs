using LiteUa.Security.Policies;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Tests.UnitTests.Security.Policies
{
    [Trait("Category", "Unit")]
    public class SecurityPolicyBasic256Sha256Tests : IDisposable
    {
        private readonly X509Certificate2 _certA;
        private readonly X509Certificate2 _certB;

        private readonly SecurityPolicyBasic256Sha256 _policyClient; // Local=A, Remote=B
        private readonly SecurityPolicyBasic256Sha256 _policyServer; // Local=B, Remote=A

        public SecurityPolicyBasic256Sha256Tests()
        {
            _certA = CreateSelfSignedCertificate("Client");
            _certB = CreateSelfSignedCertificate("Server");

            // Client: Signs with A, Encrypts for B
            _policyClient = new SecurityPolicyBasic256Sha256(_certA, _certB);

            // Server: Signs with B, Encrypts for A
            _policyServer = new SecurityPolicyBasic256Sha256(_certB, _certA);
        }

        [Fact]
        public void AsymmetricSignVerify_CrossInstance_Success()
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello OPC UA");

            // Client signs with A
            byte[] signature = _policyClient.Sign(data);

            // Server verifies with A
            bool isValid = _policyServer.Verify(data, signature);

            Assert.True(isValid);
        }

        [Fact]
        public void AsymmetricEncryption_SingleBlock_CrossInstance_Success()
        {
            byte[] plainText = new byte[214];
            RandomNumberGenerator.Fill(plainText);

            // Client encrypts for B
            byte[] cipherText = _policyClient.EncryptAsymmetric(plainText);

            // Server decrypts with B
            byte[] decrypted = _policyServer.DecryptAsymmetric(cipherText);

            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void AsymmetricEncryption_MultiBlock_CrossInstance_Success()
        {
            byte[] plainText = new byte[428]; // 2 blocks
            RandomNumberGenerator.Fill(plainText);

            byte[] cipherText = _policyClient.EncryptAsymmetric(plainText);
            byte[] decrypted = _policyServer.DecryptAsymmetric(cipherText);

            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void SymmetricOperations_CrossInstance_Success()
        {
            byte[] clientNonce = new byte[32];
            byte[] serverNonce = new byte[32];
            RandomNumberGenerator.Fill(clientNonce);
            RandomNumberGenerator.Fill(serverNonce);

            // Client side: Standard order
            _policyClient.DeriveKeys(clientNonce, serverNonce);

            // Server side: SWAP nonces so Server's 'Receiving' matches Client's 'Sending'
            _policyServer.DeriveKeys(serverNonce, clientNonce);

            byte[] plainText = new byte[32];
            RandomNumberGenerator.Fill(plainText);

            // Client Encrypts (SendingKeys) -> Server Decrypts (ReceivingKeys)
            byte[] cipherText = _policyClient.EncryptSymmetric(plainText);
            byte[] decrypted = _policyServer.DecryptSymmetric(cipherText);

            Assert.Equal(plainText, decrypted);

            // Client Signs (SendingKeys) -> Server Verifies (ReceivingKeys)
            byte[] signature = _policyClient.SignSymmetric(plainText);
            bool isValid = _policyServer.VerifySymmetric(plainText, signature);
            Assert.True(isValid);
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
                DateTimeOffset.UtcNow.AddYears(25));

            string certPem = certEphemeral.ExportCertificatePem();
            string keyPem = rsa.ExportPkcs8PrivateKeyPem();
            return X509Certificate2.CreateFromPem(certPem, keyPem);
        }

        public void Dispose()
        {
            _certA.Dispose();
            _certB.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}