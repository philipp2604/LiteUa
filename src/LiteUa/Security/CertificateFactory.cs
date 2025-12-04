using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

/// TODO: Add unit tests

namespace LiteUa.Security
{
    /// <summary>
    /// A factory class for creating self-signed X.509 certificates.
    /// </summary>
    public static class CertificateFactory
    {
        /// <summary>
        /// Creates a self-signed X.509 certificate with the specified application name and URI.
        /// </summary>
        /// <param name="applicationName">The application name to use in the certificate.</param>
        /// <param name="applicationUri">The application uri to use in the certificate.</param>
        /// <returns>A new instance of a <see cref="X509Certificate2"/>.</returns>
        public static X509Certificate2 CreateSelfSignedCertificate(string applicationName, string applicationUri)
        {
            // 1. Create rsa key
            using var rsa = RSA.Create(2048);

            // 2. Create cert request
            var request = new CertificateRequest(
                $"CN={applicationName}, DC={Dns.GetHostName()}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // add extensions
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddUri(new Uri(applicationUri));
            sanBuilder.AddDnsName(Dns.GetHostName());
            request.CertificateExtensions.Add(sanBuilder.Build());

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature |
                    X509KeyUsageFlags.KeyEncipherment |
                    X509KeyUsageFlags.DataEncipherment |
                    X509KeyUsageFlags.KeyCertSign,
                    true));

            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection {
                        new Oid("1.3.6.1.5.5.7.3.1"), // Server Auth
                        new Oid("1.3.6.1.5.5.7.3.2")  // Client Auth
                    },
                    false));

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));

            // 3. Create ephemeral self-signed cert
            using var certEphemeral = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(25));

            // Reimport cert to get a cert with associated private key
            string certPem = certEphemeral.ExportCertificatePem();
            string keyPem = rsa.ExportPkcs8PrivateKeyPem();
            var newCert = X509Certificate2.CreateFromPem(certPem, keyPem);

            return newCert;
        }
    }
}
