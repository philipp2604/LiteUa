using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Attribute
{
    /// <summary>
    /// Represents a ReadValueId structure used to specify the node and attribute to read in an OPC UA server.
    /// </summary>
    /// <param name="nodeId"></param>
    public class ReadValueId(NodeId nodeId)
    {
        /// <summary>
        /// Gets or sets the NodeId of the node to read.
        /// </summary>
        public NodeId NodeId { get; set; } = nodeId;

        /// <summary>
        /// Gets or sets the AttributeId to read. Default is 13 (Value attribute).
        /// </summary>
        public uint AttributeId { get; set; } = 13; // Value

        /// <summary>
        /// Gets or sets the IndexRange to read a subset of an array. Null means the entire array.
        /// </summary>
        public string? IndexRange { get; set; } // null

        /// <summary>
        /// Gets or sets the DataEncoding to use. Null means the default encoding.
        /// </summary>
        public QualifiedName? DataEncoding { get; set; } // null

        /// <summary>
        /// Encodes the ReadValueId using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            writer.WriteUInt32(AttributeId);
            writer.WriteString(IndexRange);

            if (DataEncoding == null)
            {
                writer.WriteUInt16(0); // NS 0
                writer.WriteString(null); // Name null
            }
            else
            {
                DataEncoding.Encode(writer);
            }
        }
    }
}