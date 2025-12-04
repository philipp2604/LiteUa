using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Security.Policies
{
    /// <summary>
    /// A class representing the "None" security policy in OPC UA.
    /// </summary>
    public class SecurityPolicyNone : ISecurityPolicy
    {
        public string SecurityPolicyUri => "http://opcfoundation.org/UA/SecurityPolicy#None";

        public int AsymmetricSignatureSize => 0;
        public int AsymmetricEncryptionBlockSize => 1;
        public int AsymmetricCipherTextBlockSize => 1;

        public int SymmetricSignatureSize => 0;
        public int SymmetricBlockSize => 1;
        public int SymmetricKeySize => 0;
        public int SymmetricInitializationVectorSize => 0;

        public byte[] Sign(byte[] dataToSign) => Array.Empty<byte>();
        public bool Verify(byte[] dataToVerify, byte[] signature) => true;
        public byte[] EncryptAsymmetric(byte[] dataToEncrypt) => dataToEncrypt;
        public byte[] DecryptAsymmetric(byte[] dataToDecrypt) => dataToDecrypt;

        public void DeriveKeys(byte[] clientNonce, byte[] serverNonce) {}

        public byte[] SignSymmetric(byte[] dataToSign) => Array.Empty<byte>();
        public bool VerifySymmetric(byte[] dataToVerify, byte[] signature) => true;
        public byte[] EncryptSymmetric(byte[] dataToEncrypt) => dataToEncrypt;
        public byte[] DecryptSymmetric(byte[] dataToDecrypt) => dataToDecrypt;
    }
}
