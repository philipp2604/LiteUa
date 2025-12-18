using LiteUa.Encoding;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;

namespace LiteUa.Stack.Discovery
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class EndpointDescription
    {
        public string? EndpointUrl { get; set; }
        public ApplicationDescription? Server { get; set; }
        public byte[]? ServerCertificate { get; set; }
        public MessageSecurityMode SecurityMode { get; set; }
        public string? SecurityPolicyUri { get; set; }
        public UserTokenPolicy[]? UserIdentityTokens { get; set; }
        public string? TransportProfileUri { get; set; }
        public byte SecurityLevel { get; set; }

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