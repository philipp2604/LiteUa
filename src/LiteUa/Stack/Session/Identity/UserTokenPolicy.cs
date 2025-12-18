using LiteUa.Encoding;

namespace LiteUa.Stack.Session.Identity
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class UserTokenPolicy
    {
        public string? PolicyId { get; set; }
        public int TokenType { get; set; }
        public string? IssuedTokenType { get; set; }
        public string? IssuerEndpointUrl { get; set; }
        public string? SecurityPolicyUri { get; set; }

        public static UserTokenPolicy Decode(OpcUaBinaryReader reader)
        {
            return new UserTokenPolicy
            {
                PolicyId = reader.ReadString(),
                TokenType = reader.ReadInt32(),
                IssuedTokenType = reader.ReadString(),
                IssuerEndpointUrl = reader.ReadString(),
                SecurityPolicyUri = reader.ReadString()
            };
        }
    }
}