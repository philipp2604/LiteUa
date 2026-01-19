using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represents a target node and the index of the remaining path in a browse path operation.
    /// </summary>
    public class BrowsePathTarget
    {
        /// <summary>
        /// Gets or sets the identifier of the target node.
        /// </summary>
        public ExpandedNodeId? TargetId { get; set; }

        /// <summary>
        /// Gets or sets the index of the next segment to process in the path.
        /// </summary>
        public uint RemainingPathIndex { get; set; }

        /// <summary>
        /// Decodes a <see cref="BrowsePathTarget"/> instance using the specified OPC UA binary reader.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for reading the encoded <see cref="BrowsePathTarget"/> data. Cannot be
        /// null.</param>
        /// <returns>A <see cref="BrowsePathTarget"/> object decoded from the binary stream.</returns>
        public static BrowsePathTarget Decode(OpcUaBinaryReader reader)
        {
            return new BrowsePathTarget
            {
                TargetId = ExpandedNodeId.Decode(reader),
                RemainingPathIndex = reader.ReadUInt32()
            };
        }
    }
}