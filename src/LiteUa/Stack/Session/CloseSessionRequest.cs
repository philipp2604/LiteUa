using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Session
{
    /// <summary>
    /// Represents a CloseSessionRequest message used to close a session in OPC UA.
    /// </summary>
    public class CloseSessionRequest
    {
        /// <summary>
        /// Gets the NodeId for the CloseSessionRequest message.
        /// </summary>
        public static readonly NodeId NodeId = new(473);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> for the CloseSessionRequest message.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets a value indicating whether to delete subscriptions associated with the session.
        /// </summary>
        public bool DeleteSubscriptions { get; set; } = true;

        /// <summary>
        /// Encodes the CloseSessionRequest message using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteBoolean(DeleteSubscriptions);
        }
    }
}