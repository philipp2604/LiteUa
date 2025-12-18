using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Session
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class ActivateSessionRequest
    {
        public static readonly NodeId NodeId = new(467);

        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        public SignatureData ClientSignature { get; set; } = new SignatureData();
        public string[]? ClientSoftwareCertificates { get; set; } // Placeholder
        public string[]? LocaleIds { get; set; }
        public ExtensionObject? UserIdentityToken { get; set; } // anonymous or username
        public SignatureData UserTokenSignature { get; set; } = new SignatureData(); // only needed for certificate based auth

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            ClientSignature.Encode(writer);

            // ClientSoftwareCertificates (Empty Array)
            writer.WriteInt32(0);

            // LocaleIds
            if (LocaleIds == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(LocaleIds.Length);
                foreach (var l in LocaleIds) writer.WriteString(l);
            }

            // UserIdentityToken
            if (UserIdentityToken == null) throw new System.Exception("UserIdentityToken must be set");
            UserIdentityToken.Encode(writer);

            // UserTokenSignature
            UserTokenSignature.Encode(writer);
        }
    }
}