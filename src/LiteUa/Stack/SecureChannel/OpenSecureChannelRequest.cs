using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.SecureChannel
{
    /// <summary>
    /// Represents an OpenSecureChannelRequest message used to establish a secure channel in OPC UA.
    /// </summary>
    public class OpenSecureChannelRequest
    {
        /// <summary>
        /// Gets the NodeId for the OpenSecureChannelRequest message.
        /// </summary>
        public static readonly NodeId NodeId = new(446);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> containing metadata about the request.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the client protocol version.
        /// </summary>
        public uint ClientProtocolVersion { get; set; } = 0;

        /// <summary>
        /// Gets or sets the type of security token request.
        /// </summary>
        public SecurityTokenRequestType RequestType { get; set; } = SecurityTokenRequestType.Issue;

        /// <summary>
        /// Gets or sets the message security mode.
        /// </summary>
        public MessageSecurityMode SecurityMode { get; set; } = MessageSecurityMode.None;

        /// <summary>
        /// Gets or sets the client nonce (a random number used once).
        /// </summary>
        public byte[]? ClientNonce { get; set; }

        /// <summary>
        /// Gets or sets the requested lifetime of the secure channel in milliseconds.
        /// </summary>
        public uint RequestedLifetime { get; set; } = 3600000; // 1h

        /// <summary>
        /// Encodes the OpenSecureChannelRequest using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteUInt32(ClientProtocolVersion);
            writer.WriteInt32((int)RequestType);
            writer.WriteInt32((int)SecurityMode);
            writer.WriteByteString(ClientNonce);
            writer.WriteUInt32(RequestedLifetime);
        }
    }
}