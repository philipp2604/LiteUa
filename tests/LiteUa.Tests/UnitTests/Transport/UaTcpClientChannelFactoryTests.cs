using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Transport;
using Moq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Tests.UnitTests.Transport
{
    [Trait("Category", "Unit")]
    public class UaTcpClientChannelFactoryTests
    {
        private readonly UaTcpClientChannelFactory _factory;
        private readonly Mock<ISecurityPolicyFactory> _policyFactoryMock;

        public UaTcpClientChannelFactoryTests()
        {
            _factory = new UaTcpClientChannelFactory();
            _policyFactoryMock = new Mock<ISecurityPolicyFactory>();
        }

        [Fact]
        public void CreateTcpClientChannel_ReturnsCorrectConcreteType()
        {
            // Arrange
            string url = "opc.tcp://localhost:4840";
            string appUri = "urn:test:client";
            string prodUri = "urn:test:product";
            string appName = "TestApp";
            var mode = MessageSecurityMode.None;

            // Act
            var channel = _factory.CreateTcpClientChannel(
                url,
                appUri,
                prodUri,
                appName,
                _policyFactoryMock.Object,
                mode,
                null,
                null,
                20000,
                10000);

            // Assert
            Assert.NotNull(channel);
            Assert.IsType<UaTcpClientChannel>(channel);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateTcpClientChannel_InvalidUrl_ThrowsException(string? invalidUrl)
        {
            // Arrange
            var policyMock = new Mock<ISecurityPolicyFactory>();

            // Act & Assert
            Assert.ThrowsAny<ArgumentException>(() => _factory.CreateTcpClientChannel(
                invalidUrl!,
                "uri",
                "prod",
                "name",
                policyMock.Object,
                MessageSecurityMode.None,
                null,
                null,
                20000,
                10000));
        }

        [Fact]
        public void CreateTcpClientChannel_NullPolicyFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _factory.CreateTcpClientChannel(
                "opc.tcp://url",
                "uri",
                "prod",
                "name",
                null!,
                MessageSecurityMode.None,
                null,
                null,
                20000,
                10000));
        }

        [Fact]
        public void CreateTcpClientChannel_AcceptsCertificates()
        {
            // Arrange
            using var clientCert = CreateSelfSignedCertificate("ClientCert");
            using var serverCert = CreateSelfSignedCertificate("ServerCert");

            // Act
            var channel = _factory.CreateTcpClientChannel(
                "opc.tcp://localhost",
                "uri",
                "prod",
                "name",
                _policyFactoryMock.Object,
                MessageSecurityMode.SignAndEncrypt,
                clientCert,
                serverCert,
                20000,
                10000);

            // Assert
            Assert.NotNull(channel);
            Assert.IsType<UaTcpClientChannel>(channel);
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
    }
}