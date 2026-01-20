using LiteUa.BuiltIn;
using LiteUa.Encoding;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Stack.Session.Identity
{
    /// <summary>
    /// Represents an anonymous user identity token.
    /// </summary>
    /// <param name="policyId">The policy id string.</param>
    public class AnonymousIdentityToken(string policyId = "Anonymous") : IUserIdentity
    {
        /// <summary>
        /// Gets or sets the policy id of the anonymous identity token.
        /// </summary>
        public string PolicyId { get; set; } = policyId;

        public ExtensionObject ToExtensionObject(X509Certificate2? serverCertificate, byte[]? serverNonce)
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