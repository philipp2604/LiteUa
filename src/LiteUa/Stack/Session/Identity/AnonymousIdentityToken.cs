using LiteUa.BuiltIn;
using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Session.Identity
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class AnonymousIdentityToken(string policyId) : IUserIdentity
    {
        public string PolicyId { get; set; } = policyId;

        public ExtensionObject ToExtensionObject(X509Certificate2 serverCertificate, byte[] serverNonce)
        {
            var ext = new ExtensionObject
            {
                TypeId = new NodeId(321),
                Encoding = 0x01
            };
            using (var ms = new System.IO.MemoryStream())
            {
                var w = new OpcUaBinaryWriter(ms);
                w.WriteString(PolicyId);
                ext.Body = ms.ToArray();
            }
            return ext;
        }
    }
}
