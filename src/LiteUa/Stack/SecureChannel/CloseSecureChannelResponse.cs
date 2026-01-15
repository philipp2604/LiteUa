using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.SecureChannel
{
    /// <summary>
    /// Represents a CloseSecureChannelResponse message in OPC UA.
    /// </summary>
    public class CloseSecureChannelResponse
    {
        /// <summary>
        /// Gets the NodeId for the CloseSecureChannelResponse message.
        /// </summary>
        public static readonly NodeId NodeId = new(455);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> of the CloseSecureChannelResponse.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Decodes the <see cref="CloseSecureChannelResponse"/> using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);
        }
    }
}