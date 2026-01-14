using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Attribute
{
    /// <summary>
    /// Represents a value to write to a node's attribute.
    /// </summary>
    /// <param name="nodeId">The NodeId to write to.</param>
    /// <param name="value">The value to write.</param>
    public class WriteValue(NodeId nodeId, DataValue value)
    {
        /// <summary>
        /// Gets or sets the NodeId of the node to write to.
        /// </summary>
        public NodeId NodeId { get; set; } = nodeId ?? throw new ArgumentNullException(nameof(nodeId));

        /// <summary>
        /// Gets or sets the AttributeId of the attribute to write to, default is 13 (Value).
        /// </summary>
        public uint AttributeId { get; set; } = 13; // Value

        /// <summary>
        /// Gets or sets the IndexRange for array attributes, null if not used.
        /// </summary>
        public string? IndexRange { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataValue"/> to write.
        /// </summary>
        public DataValue Value { get; set; } = value ?? throw new ArgumentNullException(nameof(value));

        /// <summary>
        /// Emcodes the WriteValue using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            writer.WriteUInt32(AttributeId);
            writer.WriteString(IndexRange);
            Value.Encode(writer);
        }
    }
}