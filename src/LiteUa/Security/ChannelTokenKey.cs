using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Security
{
    /// <summary>
    /// A class representing the keys used for signing and encrypting channel tokens.
    /// </summary>
    /// <param name="signingKey">The signing key.</param>
    /// <param name="encryptingKey">The encrypting key.</param>
    /// <param name="iv">The initialization vector.</param>
    public class ChannelTokenKeys(byte[] signingKey, byte[] encryptingKey, byte[] iv)
    {
        /// <summary>
        /// Gets the signing key.
        /// </summary>
        public byte[] SigningKey { get; } = signingKey;

        /// <summary>
        /// Gets the encrypting key.
        /// </summary>
        public byte[] EncryptingKey { get; } = encryptingKey;

        /// <summary>
        /// Gets the initialization vector.
        /// </summary>
        public byte[] InitializationVector { get; } = iv;
    }
}
