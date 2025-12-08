using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

/// TODO: Add unit tests

namespace LiteUa.Security.Policies
{
    public class SecurityPolicyBasic256Sha256 : ISecurityPolicy
    {
        public string SecurityPolicyUri => "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256";

        private readonly X509Certificate2 _localCertificate; // Private Key for signing/decrypting
        private readonly X509Certificate2 _remoteCertificate; // Public Key for encrypting/verifying

        // Asymmetric algorithms
        private readonly RSA _localRsa;
        private readonly RSA _remoteRsa;

        // Symmetric keys
        private ChannelTokenKeys? _sendingKeys;
        private ChannelTokenKeys? _receivingKeys;

        public SecurityPolicyBasic256Sha256(X509Certificate2 localCertificate, X509Certificate2 remoteCertificate)
        {
            _localCertificate = localCertificate ?? throw new ArgumentNullException(nameof(localCertificate));
            _remoteCertificate = remoteCertificate ?? throw new ArgumentNullException(nameof(remoteCertificate));

            // Get RSA providers
            _localRsa = _localCertificate.GetRSAPrivateKey() ?? throw new ArgumentNullException(nameof(localCertificate));
            _remoteRsa = _remoteCertificate.GetRSAPublicKey() ?? throw new ArgumentNullException(nameof(remoteCertificate));

            if (_localRsa == null) throw new Exception("Local certificate has no private key!");
        }

        // --- Asymmetric Config ---
        // Basic256Sha256 needs RSA 2048 Min.
        // KeySize (bits) / 8 = Bytes. 2048 / 8 = 256 Bytes.
        public int AsymmetricSignatureSize => _localRsa.KeySize / 8;
        public int AsymmetricCipherTextBlockSize => _remoteRsa.KeySize / 8;

        // Plaintext Block Size for RSA OAEP SHA1: KeySize - 41 (Overhead) - 1
        // With 2048 bit (256 byte) -> 214 bytes max input
        public int AsymmetricEncryptionBlockSize => (AsymmetricCipherTextBlockSize - 42);

        // --- Symmetric Config ---
        public int SymmetricSignatureSize => 32; // HMAC-SHA256 = 256 bit = 32 byte
        public int SymmetricBlockSize => 16;     // AES Block = 128 bit = 16 byte
        public int SymmetricKeySize => 32;       // AES-256 = 32 byte Key
        public int SymmetricInitializationVectorSize => 16; // AES IV

        // --------------------------------------------------------------------------------
        // Asymmetric Implementation
        // --------------------------------------------------------------------------------

        public byte[] Sign(byte[] dataToSign)
        {
            // Basic256Sha256 uses RSA-PKCS15-SHA256 for signatures
            return _localRsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public bool Verify(byte[] dataToVerify, byte[] signature)
        {
            return _remoteRsa.VerifyData(dataToVerify, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public byte[] EncryptAsymmetric(byte[] dataToEncrypt)
        {
            // Basic256Sha256 uses RSA-OAEP-SHA1 for encryption.
            // RSA can encrypt a maximum of (KeySize - Overhead) at once.
            // We need to split the plaintext into blocks as per OPC UA Spec.

            int inputBlockSize = AsymmetricEncryptionBlockSize; // 256
            int outputBlockSize = AsymmetricCipherTextBlockSize; // 214

            // Check if input length is a multiple of the block size (should be guaranteed by PaddingCalculator in the Channel)
            if (dataToEncrypt.Length % inputBlockSize != 0)
            {
                throw new ArgumentException($"Data length ({dataToEncrypt.Length}) is not a multiple of the input block size ({inputBlockSize}). Padding logic failed.");
            }

            int blockCount = dataToEncrypt.Length / inputBlockSize;
            byte[] cipherText = new byte[blockCount * outputBlockSize];

            for (int i = 0; i < blockCount; i++)
            {
                byte[] chunk = new byte[inputBlockSize];
                Array.Copy(dataToEncrypt, i * inputBlockSize, chunk, 0, inputBlockSize);

                // Encrypt the chunk
                byte[] encryptedChunk = _remoteRsa.Encrypt(chunk, RSAEncryptionPadding.OaepSHA1);

                // Copy into the output buffer
                Array.Copy(encryptedChunk, 0, cipherText, i * outputBlockSize, outputBlockSize);
            }

            return cipherText;
        }

        public byte[] DecryptAsymmetric(byte[] dataToDecrypt)
        {
            int inputBlockSize = AsymmetricCipherTextBlockSize; // 256
            int outputBlockSize = AsymmetricEncryptionBlockSize; // 214

            if (dataToDecrypt.Length % inputBlockSize != 0) throw new ArgumentException("Invalid length");

            int blockCount = dataToDecrypt.Length / inputBlockSize;

            // Create buffer with maximum possible size
            byte[] tempBuffer = new byte[blockCount * outputBlockSize];

            for (int i = 0; i < blockCount; i++)
            {
                byte[] chunk = new byte[inputBlockSize];
                Array.Copy(dataToDecrypt, i * inputBlockSize, chunk, 0, inputBlockSize);

                byte[] decryptedChunk = _localRsa.Decrypt(chunk, RSAEncryptionPadding.OaepSHA1);

                Array.Copy(decryptedChunk, 0, tempBuffer, i * outputBlockSize, decryptedChunk.Length);

                // Check (Debugging):
                if (decryptedChunk.Length != outputBlockSize)
                {
                    // Should not happen with correct OAEP padding and full blocks (OPC UA Spec).
                    throw new Exception("Decrypted chunk size does not match expected output block size.");
                }
            }

            return tempBuffer;
        }

        // --------------------------------------------------------------------------------
        // Key Derivation (Spec Part 6, 6.7.5)
        // --------------------------------------------------------------------------------
        public void DeriveKeys(byte[] clientNonce, byte[] serverNonce)
        {
            int sigLen = SymmetricSignatureSize;
            int encLen = SymmetricKeySize;
            int ivLen = SymmetricInitializationVectorSize;
            int totalLen = sigLen + encLen + ivLen;

            byte[] clientKeyBlock = CryptoUtils.PSha256(serverNonce, clientNonce, totalLen);

            byte[] clientSigKey = new byte[sigLen];
            byte[] clientEncKey = new byte[encLen];
            byte[] clientIV = new byte[ivLen];

            Buffer.BlockCopy(clientKeyBlock, 0, clientSigKey, 0, sigLen);
            Buffer.BlockCopy(clientKeyBlock, sigLen, clientEncKey, 0, encLen);
            Buffer.BlockCopy(clientKeyBlock, sigLen + encLen, clientIV, 0, ivLen);

            _sendingKeys = new ChannelTokenKeys(clientSigKey, clientEncKey, clientIV);

            byte[] serverKeyBlock = CryptoUtils.PSha256(clientNonce, serverNonce, totalLen);

            byte[] serverSigKey = new byte[sigLen];
            byte[] serverEncKey = new byte[encLen];
            byte[] serverIV = new byte[ivLen];

            Buffer.BlockCopy(serverKeyBlock, 0, serverSigKey, 0, sigLen);
            Buffer.BlockCopy(serverKeyBlock, sigLen, serverEncKey, 0, encLen);
            Buffer.BlockCopy(serverKeyBlock, sigLen + encLen, serverIV, 0, ivLen);

            _receivingKeys = new ChannelTokenKeys(serverSigKey, serverEncKey, serverIV);
        }

        // --------------------------------------------------------------------------------
        // Symmetric Implementation
        // --------------------------------------------------------------------------------

        public byte[] SignSymmetric(byte[] dataToSign)
        {
            if (_sendingKeys == null) throw new InvalidOperationException("Keys not derived yet.");

            using var hmac = new HMACSHA256(_sendingKeys.SigningKey);
            return hmac.ComputeHash(dataToSign);
        }

        public bool VerifySymmetric(byte[] dataToVerify, byte[] signature)
        {
            if (_receivingKeys == null) throw new InvalidOperationException("Keys not derived yet.");

            using var hmac = new HMACSHA256(_receivingKeys.SigningKey);
            byte[] computed = hmac.ComputeHash(dataToVerify);

            // Constant Time Compare, we use a simple approach here
            if (computed.Length != signature.Length) return false;
            for (int i = 0; i < computed.Length; i++)
            {
                if (computed[i] != signature[i]) return false;
            }
            return true;
        }

        public byte[] EncryptSymmetric(byte[] dataToEncrypt)
        {
            if (_sendingKeys == null) throw new InvalidOperationException("Keys not derived yet.");

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = _sendingKeys.EncryptingKey;
            aes.IV = _sendingKeys.InitializationVector;

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(dataToEncrypt, 0, dataToEncrypt.Length);
        }

        public byte[] DecryptSymmetric(byte[] dataToDecrypt)
        {
            if (_receivingKeys == null) throw new InvalidOperationException("Keys not derived yet.");

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = _receivingKeys.EncryptingKey;
            aes.IV = _receivingKeys.InitializationVector;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(dataToDecrypt, 0, dataToDecrypt.Length);
        }
    }
}
