using LiteUa.Encoding;

namespace LiteUa.Stack.Session.Identity
{
    /// <summary>
    /// Represents a UserTokenPolicy used in OPC UA for user authentication.
    /// </summary>
    public class UserTokenPolicy
    {
        /// <summary>
        /// Gets or sets the PolicyId of the UserTokenPolicy.
        /// </summary>
        public string? PolicyId { get; set; }

        /// <summary>
        /// Gets or sets the TokenType of the UserTokenPolicy.
        /// </summary>
        public int TokenType { get; set; }

        /// <summary>
        /// Gets or sets the IssuedTokenType of the UserTokenPolicy.
        /// </summary>
        public string? IssuedTokenType { get; set; }

        /// <summary>
        /// Gets or sets the IssuerEndpointUrl of the UserTokenPolicy.
        /// </summary>
        public string? IssuerEndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the SecurityPolicyUri of the UserTokenPolicy.
        /// </summary>
        public string? SecurityPolicyUri { get; set; }

        /// <summary>
        /// Decodes a UserTokenPolicy using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded <see cref="UserTokenPolicy"/> instance.</returns>
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