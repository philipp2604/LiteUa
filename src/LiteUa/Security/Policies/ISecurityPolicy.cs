using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Security.Policies
{
    /// <summary>
    /// An interface defining the contract for security policies in OPC UA.
    /// </summary>
    public interface ISecurityPolicy
    {
        /// <summary>
        /// Gets the URI of the security policy.
        /// </summary>
        string SecurityPolicyUri { get; }

        // --- Block Sizes & Lengths ---
        /// <summary>
        /// Gets the size of the asymmetric signature in bytes.
        /// </summary>
        int AsymmetricSignatureSize { get; }

        /// <summary>
        /// Get the maximum size of data that can be encrypted with the asymmetric encryption key.
        /// </summary>
        int AsymmetricEncryptionBlockSize { get; }

        /// <summary>
        /// Gets the size of the asymmetric cipher text block in bytes.
        /// </summary>
        int AsymmetricCipherTextBlockSize { get; }

        /// <summary>
        /// Gets the size of the symmetric signature in bytes.
        /// </summary>
        int SymmetricSignatureSize { get; }

        /// <summary>
        /// Gets the block size used for symmetric encryption in bytes.
        /// </summary>
        int SymmetricBlockSize { get; }

        /// <summary>
        /// Gets the size of the symmetric key in bytes.
        /// </summary>
        int SymmetricKeySize { get; }

        /// <summary>
        /// Gets the size of the symmetric initialization vector in bytes.
        /// </summary>
        int SymmetricInitializationVectorSize { get; }

        // --- Asymmetric Operations (Handshake) ---
        byte[] Sign(byte[] dataToSign);

        /// <summary>
        /// Verifies data using the asymmetric signing key.
        /// </summary>
        /// <param name="dataToVerify">The data to verify.</param>
        /// <param name="signature">The signature to use for verifying.</param>
        bool Verify(byte[] dataToVerify, byte[] signature);

        /// <summary>
        /// Encrypts data using the asymmetric encryption key.
        /// </summary>
        /// <param name="dataToEncrypt">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        byte[] EncryptAsymmetric(byte[] dataToEncrypt);

        /// <summary>
        /// Decrypts data using the asymmetric decryption key.
        /// </summary>
        /// <param name="dataToDecrypt">The data to encrypt.</param>
        /// <returns>The decrypted data.</returns>
        byte[] DecryptAsymmetric(byte[] dataToDecrypt);

        // --- Key Derivation ---
        /// <summary>
        ///  Derives the symmetric keys from the client and server nonces and saves them internally.
        /// </summary>
        /// <param name="clientNonce"></param>
        /// <param name="serverNonce"></param>
        void DeriveKeys(byte[] clientNonce, byte[] serverNonce);

        // --- Symmetric Operations (Payload) ---
        /// <summary>
        /// Signs data using the symmetric signing key.
        /// </summary>
        /// <param name="dataToSign">The data to sign using the key.</param>
        /// <returns>The signed data.</returns>
        byte[] SignSymmetric(byte[] dataToSign);

        /// <summary>
        /// Verifies data using the symmetric signing key.
        /// </summary>
        /// <param name="dataToVerify">The data to verify.</param>
        /// <param name="signature">The signature to use for verifying.</param>
        bool VerifySymmetric(byte[] dataToVerify, byte[] signature);

        /// <summary>
        /// Encrypts data using the symmetric encryption key.
        /// </summary>
        /// <param name="dataToEncrypt">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        byte[] EncryptSymmetric(byte[] dataToEncrypt);

        /// <summary>
        /// Decrypts data using the symmetric encryption key.
        /// </summary>
        /// <param name="dataToDecrypt">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        byte[] DecryptSymmetric(byte[] dataToDecrypt);
    }
}
