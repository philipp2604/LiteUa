using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Attribute
{
    /// <summary>
    /// Represents a WriteRequest message used to write values to nodes on an OPC UA server.
    /// </summary>
    public class WriteRequest
    {
        /// <summary>
        /// Gets the NodeId for the WriteRequest message type.
        /// </summary>
        public static readonly NodeId NodeId = new(673);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> containing metadata about the request.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the array of <see cref="WriteValue"/> nodes to write.
        /// </summary>
        public WriteValue[]? NodesToWrite { get; set; }

        /// <summary>
        /// Encodes the WriteRequest using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            if (NodesToWrite == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(NodesToWrite.Length);
                foreach (var node in NodesToWrite) node.Encode(writer);
            }
        }
    }
}