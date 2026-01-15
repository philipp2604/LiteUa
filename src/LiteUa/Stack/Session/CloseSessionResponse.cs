using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Session
{
    /// <summary>
    /// Represents a CloseSessionResponse message used to respond to a CloseSessionRequest in OPC UA.
    /// </summary>
    public class CloseSessionResponse
    {
        /// <summary>
        /// Gets the NodeId for the CloseSessionResponse message.
        /// </summary>
        public static readonly NodeId NodeId = new(476);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> for the CloseSessionResponse message.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Decodes a CloseSessionResponse message using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);
        }
    }
}