using LiteUa.Encoding;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;

namespace LiteUa.Stack.Discovery
{
    /// <summary>
    /// Represents an endpoint description in OPC UA.
    /// </summary>

    public class EndpointDescription
    {
        /// <summary>
        /// Gets or sets the endpoint url of the EndpointDescription.
        /// </summary>
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the Server <see cref="ApplicationDescription"/> of the EndpointDescription.
        /// </summary>
        public ApplicationDescription? Server { get; set; }

        /// <summary>
        /// Gets or sets the server certificate of the EndpointDescription.
        /// </summary>
        public byte[]? ServerCertificate { get; set; }

        /// <summary>
        /// Gets or sets the security mode of the EndpointDescription.
        /// </summary>
        public MessageSecurityMode SecurityMode { get; set; }

        /// <summary>
        /// Gets or sets the security policy URI of the EndpointDescription.
        /// </summary>
        public string? SecurityPolicyUri { get; set; }

        /// <summary>
        /// Gets or sets the user identity tokens, of type <see cref="UserTokenPolicy"/>, supported by the EndpointDescription.
        /// </summary>
        public UserTokenPolicy[]? UserIdentityTokens { get; set; }

        /// <summary>
        /// Gets or sets the transport profile URI of the EndpointDescription.
        /// </summary>
        public string? TransportProfileUri { get; set; }

        /// <summary>
        /// Gets or sets the security level of the EndpointDescription.
        /// </summary>
        public byte SecurityLevel { get; set; }

        /// <summary>
        /// Decodes an EndpointDescription using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded <see cref="EndpointDescription"/> instance.</returns>
        public static EndpointDescription Decode(OpcUaBinaryReader reader)
        {
            var ep = new EndpointDescription
            {
                EndpointUrl = reader.ReadString(),
                Server = ApplicationDescription.Decode(reader),
                ServerCertificate = reader.ReadByteString(),
                SecurityMode = (MessageSecurityMode)reader.ReadInt32(),
                SecurityPolicyUri = reader.ReadString()
            };

            int count = reader.ReadInt32();
            if (count > 0)
            {
                ep.UserIdentityTokens = new UserTokenPolicy[count];
                for (int i = 0; i < count; i++) ep.UserIdentityTokens[i] = UserTokenPolicy.Decode(reader);
            }
            else
            {
                ep.UserIdentityTokens = [];
            }

            ep.TransportProfileUri = reader.ReadString();
            ep.SecurityLevel = reader.ReadByte();

            return ep;
        }
    }
}