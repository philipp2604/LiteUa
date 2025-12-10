using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

/// TODO: Add unit tests

namespace LiteUa.Security
{
    /// <summary>
    /// A class containing cryptographic utility methods.
    /// </summary>
    public static class CryptoUtils
    {
        /// <summary>
        /// Implementation of P-SHA256 according to RFC 5246 / OPC UA Spec Part 6.
        /// </summary>
        /// <param name="secret">Secret (ClientNonce or ServerNonce)</param>
        /// <param name="seed">Seed (Label + Nonce)</param>
        /// <param name="requiredLength">Required length in Bytes</param>
        public static byte[] PSha256(byte[] secret, byte[] seed, int requiredLength)
        {
            ArgumentNullException.ThrowIfNull(secret);
            ArgumentNullException.ThrowIfNull(seed);

            byte[] output = new byte[requiredLength];
            int outputOffset = 0;

            using (var hmac = new HMACSHA256(secret))
            {
                // A(0) = seed
                byte[] a = seed;

                while (outputOffset < requiredLength)
                {
                    // A(i) = HMAC(secret, A(i-1))
                    a = hmac.ComputeHash(a);

                    // P_hash block = HMAC(secret, A(i) + seed)
                    // Wir müssen A(i) und seed konkatenieren
                    byte[] dataToHash = new byte[a.Length + seed.Length];
                    Buffer.BlockCopy(a, 0, dataToHash, 0, a.Length);
                    Buffer.BlockCopy(seed, 0, dataToHash, a.Length, seed.Length);

                    byte[] hash = hmac.ComputeHash(dataToHash);

                    // Kopieren in den Output Buffer
                    int bytesToCopy = Math.Min(hash.Length, requiredLength - outputOffset);
                    Buffer.BlockCopy(hash, 0, output, outputOffset, bytesToCopy);
                    outputOffset += bytesToCopy;
                }
            }

            return output;
        }

        /// <summary>
        /// Returns the HashAlgorithmName and RSASignaturePadding for a given URI.
        /// </summary>
        /// <param name="uri">The w3.org URI to parse.</param>
        /// <returns>The <see cref="HashAlgorithmName"/> and <see cref="RSASignaturePadding"/> for the given URI.</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static (HashAlgorithmName, RSASignaturePadding) ParseAlgorithm(string uri)
        {
            return uri switch
            {
                "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256" => (HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                "http://www.w3.org/2000/09/xmldsig#rsa-sha1" => (HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1),
                _ => throw new NotSupportedException($"Unsupported Signature Algorithm: {uri}"),
            };
        }
    }
}
