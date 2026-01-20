using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represents a browse path in the OPC UA address space, consisting of a starting node and a relative path used to
    /// navigate from that node.
    /// </summary>
    /// <param name="startingNode">The node identifier that serves as the starting point for the browse path. Cannot be null.</param>
    /// <param name="relPath">The relative path that defines the sequence of references to follow from the starting node. Cannot be null.</param>
    public class BrowsePath(NodeId startingNode, RelativePath relPath)
    {
        /// <summary>
        /// Gets or sets the identifier of the node from which to start the operation.
        /// </summary>
        public NodeId StartingNode { get; set; } = startingNode;

        /// <summary>
        /// Gets or sets the relative path associated with this instance.
        /// </summary>
        public RelativePath RelativePath { get; set; } = relPath;

        /// <summary>
        /// Encodes the current object using the specified <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding the object's data. Cannot be null.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            StartingNode.Encode(writer);
            RelativePath.Encode(writer);
        }
    }
}