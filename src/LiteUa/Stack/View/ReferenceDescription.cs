using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represents a description of a reference in an OPC UA address space, including its type, direction, target node, and associated metadata.
    /// </summary>

    public class ReferenceDescription
    {
        /// <summary>
        /// Gets or sets the identifier of the reference type.
        /// </summary>
        public NodeId? ReferenceTypeId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the reference is in the forward direction.
        /// </summary>
        public bool IsForward { get; set; }

        /// <summary>
        /// Gets or sets the expanded node identifier of the target node.
        /// </summary>
        public ExpandedNodeId? NodeId { get; set; }

        /// <summary>
        /// Gets or sets the browse name of the target node.
        /// </summary>
        public QualifiedName? BrowseName { get; set; }

        /// <summary>
        /// Gets or sets the localized display name of the target node.
        /// </summary>
        public LocalizedText? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the class of the target node.
        /// </summary>
        public uint NodeClass { get; set; } // Enum NodeClass

        /// <summary>
        /// Gets or sets the type definition of the target node.
        /// </summary>
        public ExpandedNodeId? TypeDefinition { get; set; }

        /// <summary>
        /// Decodes a <see cref="ReferenceDescription"/> using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>A new instance of <see cref="ReferenceDescription"/> decoded from the binary data.</returns>
        public static ReferenceDescription Decode(OpcUaBinaryReader reader)
        {
            return new ReferenceDescription
            {
                ReferenceTypeId = BuiltIn.NodeId.Decode(reader),
                IsForward = reader.ReadBoolean(),
                NodeId = ExpandedNodeId.Decode(reader),
                BrowseName = QualifiedName.Decode(reader),
                DisplayName = LocalizedText.Decode(reader),
                NodeClass = reader.ReadUInt32(),
                TypeDefinition = ExpandedNodeId.Decode(reader)
            };
        }
    }
}