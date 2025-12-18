using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Attribute
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class WriteValue(NodeId nodeId, DataValue value)
    {
        public NodeId NodeId { get; set; } = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        public uint AttributeId { get; set; } = 13; // Value
        public string? IndexRange { get; set; }
        public DataValue Value { get; set; } = value ?? throw new ArgumentNullException(nameof(value));

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            writer.WriteUInt32(AttributeId);
            writer.WriteString(IndexRange);
            Value.Encode(writer);
        }
    }
}