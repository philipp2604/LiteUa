using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represents a BrowseDescription used in OPC UA browsing operations.
    /// </summary>
    /// <param name="nodeId"></param>
    public class BrowseDescription(NodeId nodeId)
    {
        /// <summary>
        /// Gets or sets the unique identifier for the node.
        /// </summary>
        public NodeId NodeId { get; set; } = nodeId ?? throw new ArgumentNullException(nameof(nodeId));

        /// <summary>
        /// Gets or sets the direction in which to browse items.
        /// </summary>
        public BrowseDirection BrowseDirection { get; set; } = BrowseDirection.Forward;

        /// <summary>
        /// Gets or sets the identifier of the reference type associated with this node, default is HierarchicalReferences (13).
        /// </summary>
        public NodeId ReferenceTypeId { get; set; } = new NodeId(33); // HierarchicalReferences

        /// <summary>
        /// Gets or sets a value indicating whether subtypes are included in the operation.
        /// </summary>
        public bool IncludeSubtypes { get; set; } = true;

        /// <summary>
        /// Gets or sets a bitmask that specifies which node classes to include in the operation.
        /// </summary>
        /// <remarks>A value of 0 includes all node classes. Each bit in the mask corresponds to a
        /// specific node class; set one or more bits to filter the results to those node classes only.</remarks>
        public uint NodeClassMask { get; set; } = 0; // 0 = All

        /// <summary>
        /// Gets or sets the bitmask that specifies which result fields are included in the operation output.
        /// </summary>
        public uint ResultMask { get; set; } = 63; // All

        /// <summary>
        /// Encodes the current object using the specified OPC UA binary writer.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding the object's data. Cannot be null.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            writer.WriteInt32((int)BrowseDirection);
            ReferenceTypeId.Encode(writer);
            writer.WriteBoolean(IncludeSubtypes);
            writer.WriteUInt32(NodeClassMask);
            writer.WriteUInt32(ResultMask);
        }
    }
}