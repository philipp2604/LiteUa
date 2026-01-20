using LiteUa.Encoding;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represents a relative path in an OPC UA address space, consisting of a sequence of relative path elements that define the navigation steps from a starting node to a target node.
    /// </summary>
    /// <param name="elements">The collection of relative path elements that make up the relative path. Cannot be null.</param>
    public class RelativePath(RelativePathElement[] elements)
    {
        /// <summary>
        /// Gets or sets the collection of relative path elements associated with this instance.
        /// </summary>
        public RelativePathElement[] Elements { get; set; } = elements;

        /// <summary>
        /// Encodes the <see cref="RelativePath"/> instance into a binary format using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer"></param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            if (Elements == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(Elements.Length);
                foreach (var e in Elements) e.Encode(writer);
            }
        }
    }
}