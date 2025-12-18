using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Client.Events
{
    /// TODO: Add unit tests
    /// TODI: Add ToString() method
    /// <summary>
    /// Creates a new CertificateValidationEventArgs instance.
    /// </summary>
    /// <param name="cert">The server's certificate.</param>
    /// <param name="host">The connected hostname.</param>
    /// <param name="errors">Chain erros from the OS validation.</param>
    /// <param name="passed">Whether the OS validation passed.</param>
    public class CertificateValidationEventArgs(X509Certificate2 cert, string host, X509ChainStatus[] errors, bool passed) : EventArgs
    {
        /// <summary>
        /// Gets the server certificate.
        /// </summary>
        public X509Certificate2 Certificate { get; } = cert;

        /// <summary>
        /// Gets the connected hostname.
        /// </summary>
        public string RequestHostname { get; } = host;

        /// <summary>
        /// Gets the chain errors from the OS validation.
        /// </summary>
        public X509ChainStatus[] ChainErrors { get; } = errors;

        /// <summary>
        /// Gets whether the OS validation passed.
        /// </summary>
        public bool OsValidationPassed { get; } = passed;

        /// <summary>
        /// Gets or sets whether to accept the certificate.
        /// </summary>
        public bool Accept { get; set; } = passed; // Default: rely on OS validation
    }
}