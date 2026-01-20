using LiteUa.BuiltIn;
using LiteUa.Encoding;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Stack.Session.Identity
{

    /// TODO: Implement different encryption algorithms based on security policy

    /// <summary>
    /// Represents a UserNameIdentityToken for OPC UA user authentication.
    /// </summary>
    /// <param name="policyId">The policy Id string.</param>
    /// <param name="username">The username string.</param>
    /// <param name="password">The password string.</param>
    public class UserNameIdentityToken(string policyId, string username, string password) : IUserIdentity
    {
        /// <summary>
        /// Gets or sets the PolicyId for the UserNameIdentityToken.
        /// </summary>
        public string PolicyId { get; set; } = policyId;

        /// <summary>
        /// Gets or sets the UserName for the UserNameIdentityToken.
        /// </summary>
        public string UserName { get; set; } = username;

        /// <summary>
        /// Gets or sets the Password for the UserNameIdentityToken.
        /// </summary>
        public string Password { get; set; } = password;

        /// <summary>
        /// Gets the encryption algorithm URI used for encrypting the password. Currently only RSA-OAEP is supported.
        /// </summary>

        private const string EncryptionAlgo = "http://www.w3.org/2001/04/xmlenc#rsa-oaep";

        public ExtensionObject ToExtensionObject(X509Certificate2? serverCertificate, byte[]? serverNonce)
        {
            ArgumentNullException.ThrowIfNull(serverCertificate);
            if (serverNonce == null || serverNonce.Length == 0) throw new ArgumentNullException(nameof(serverNonce));

            var ext = new ExtensionObject
            {
                TypeId = new NodeId(324),
                Encoding = 0x01
            };

            using (var ms = new System.IO.MemoryStream())
            {
                var w = new OpcUaBinaryWriter(ms);
                w.WriteString(PolicyId);
                w.WriteString(UserName);

                byte[] pwBytes = System.Text.Encoding.UTF8.GetBytes(Password ?? string.Empty);
                int pwLength = pwBytes.Length;
                int nonceLength = serverNonce.Length;

                int lengthField = pwLength + nonceLength;

                byte[] dataToEncrypt = new byte[4 + pwLength + nonceLength];

                byte[] lenBytes = new byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(lenBytes, lengthField);
                Array.Copy(lenBytes, 0, dataToEncrypt, 0, 4);

                if (pwLength > 0)
                {
                    Array.Copy(pwBytes, 0, dataToEncrypt, 4, pwLength);
                }

                Array.Copy(serverNonce, 0, dataToEncrypt, 4 + pwLength, nonceLength);

                using (var rsa = serverCertificate.GetRSAPublicKey())
                {
                    if (rsa == null) throw new InvalidOperationException("Server certificate does not have a valid RSA public key.");

                    byte[] encryptedPassword = rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.OaepSHA1);
                    w.WriteByteString(encryptedPassword);
                }

                w.WriteString(EncryptionAlgo);

                ext.Body = ms.ToArray();
            }
            return ext;
        }
    }
}