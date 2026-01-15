using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Session
{
    /// <summary>
    /// Represents an ActivateSessionRequest message used to activate a session in OPC UA.
    /// </summary>
    public class ActivateSessionRequest
    {
        /// <summary>
        /// Gets the NodeId for the ActivateSessionRequest message.
        /// </summary>
        public static readonly NodeId NodeId = new(467);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> for the ActivateSessionRequest message.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the <see cref="SignatureData"/> for the client signature.
        /// </summary>
        public SignatureData ClientSignature { get; set; } = new SignatureData();

        /// <summary>
        /// Gets or sets the array of client software certificates.
        /// </summary>
        public string[]? ClientSoftwareCertificates { get; set; } // Placeholder for future use

        /// <summary>
        /// Gets or sets the array of locale IDs.
        /// </summary>
        public string[]? LocaleIds { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ExtensionObject"/> representing the user identity token. Currently only anonymous or username token is supported.
        /// </summary>
        public ExtensionObject? UserIdentityToken { get; set; } // anonymous or username

        /// <summary>
        /// Gets or sets the <see cref="SignatureData"/> for the user token signature.
        /// </summary>
        public SignatureData UserTokenSignature { get; set; } = new SignatureData(); // only needed for certificate based auth

        /// <summary>
        /// Encodes the ActivateSessionRequest message using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        /// <exception cref="InvalidOperationException"></exception>
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
            if (UserIdentityToken == null) throw new InvalidOperationException("UserIdentityToken must be set");
            UserIdentityToken.Encode(writer);

            // UserTokenSignature
            UserTokenSignature.Encode(writer);
        }
    }
}