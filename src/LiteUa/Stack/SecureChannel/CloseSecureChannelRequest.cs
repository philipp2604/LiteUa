using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.SecureChannel
{
    /// <summary>
    /// Represents a CloseSecureChannelRequest message used to close a secure channel in OPC UA.
    /// </summary>
    public class CloseSecureChannelRequest
    {
        /// <summary>
        /// Gets the NodeId for the CloseSecureChannelRequest message.
        /// </summary>
        public static readonly NodeId NodeId = new(452);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> for the CloseSecureChannelRequest message.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Encodes the <see cref="CloseSecureChannelRequest"/> message using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
        }
    }
}