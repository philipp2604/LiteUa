using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Security;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.SecureChannel
{
    /// <summary>
    /// Represents an OpenSecureChannelResponse message in OPC UA.
    /// </summary>
    public class OpenSecureChannelResponse
    {
        /// <summary>
        /// Gets the NodeId for the OpenSecureChannelResponse type.
        /// </summary>
        public static readonly NodeId NodeId = new(449);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> of the OpenSecureChannelResponse.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the server's protocol version.
        /// </summary>
        public uint ServerProtocolVersion { get; set; }

        /// <summary>
        ///  Gets or sets the <see cref="ChannelSecurityToken"/> for the secure channel.
        /// </summary>
        public ChannelSecurityToken? SecurityToken { get; set; }

        /// <summary>
        /// Gets or sets the server nonce used in the secure channel establishment.
        /// </summary>
        public byte[]? ServerNonce { get; set; }

        /// <summary>
        /// Decodes an OpenSecureChannelResponse using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);
            ServerProtocolVersion = reader.ReadUInt32();
            SecurityToken = ChannelSecurityToken.Decode(reader);
            ServerNonce = reader.ReadByteString();
        }
    }
}