using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Attribute
{
    /// <summary>
    /// Represents a ReadRequest message used to read attribute values from OPC UA nodes.
    /// </summary>
    public class ReadRequest
    {
        /// <summary>
        /// Gets the NodeId for the ReadRequest message.
        /// </summary>
        public static readonly NodeId NodeId = new(631);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> for the ReadRequest message.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the maximum age of the value to be read, in milliseconds.
        /// </summary>
        public double MaxAge { get; set; } = 0;

        /// <summary>
        /// Gets or sets the timestamps to return for the read operation.
        /// </summary>
        public TimestampsToReturn TimestampsToReturn { get; set; } = TimestampsToReturn.Both;

        /// <summary>
        /// Gets or sets the array of <see cref="ReadValueId"/> nodes to read.
        /// </summary>
        public ReadValueId[]? NodesToRead { get; set; }

        /// <summary>
        /// Encodes the ReadRequest message using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            writer.WriteDouble(MaxAge);
            writer.WriteUInt32((uint)TimestampsToReturn);

            // Array of ReadValueId
            if (NodesToRead == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(NodesToRead.Length);
                foreach (var node in NodesToRead) node.Encode(writer);
            }
        }
    }
}