using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represents an element of a relative path used in OPC UA address space navigation, specifying the reference type, direction, and target name.
    /// </summary>
    public class RelativePathElement
    {
        /// <summary>
        /// Gets or sets the identifier of the reference type used for navigation, default is HierarchicalReferences (33).
        /// </summary>
        public NodeId ReferenceTypeId { get; set; } = new NodeId(33); // HierarchicalReferences

        /// <summary>
        /// Gets or sets a value indicating whether the reference is to be followed in the inverse direction.
        /// </summary>
        public bool IsInverse { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether subtypes of the reference type should be included in the navigation.
        /// </summary>
        public bool IncludeSubtypes { get; set; } = true;

        /// <summary>
        /// Gets or sets the qualified name of the target node to navigate to.
        /// </summary>
        public QualifiedName? TargetName { get; set; }

        /// <summary>
        /// Encodes the <see cref="RelativePathElement"/> using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding the <see cref="RelativePathElement"/>.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            ReferenceTypeId.Encode(writer);
            writer.WriteBoolean(IsInverse);
            writer.WriteBoolean(IncludeSubtypes);
            TargetName?.Encode(writer);
        }
    }
}