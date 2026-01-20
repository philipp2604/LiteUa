using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represents a request to translate browse paths to node IDs in an OPC UA server address space.
    /// </summary>
    public class TranslateBrowsePathsToNodeIdsRequest
    {
        /// <summary>
        /// Gets the NodeId for the TranslateBrowsePathsToNodeIdsRequest type.
        /// </summary>
        public static readonly NodeId NodeId = new(554);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> for the operation.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the collection of browse paths to be translated to node IDs.
        /// </summary>
        public BrowsePath[]? BrowsePaths { get; set; }

        /// <summary>
        /// Encodes the <see cref="TranslateBrowsePathsToNodeIdsRequest"/> using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> instance to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            if (BrowsePaths == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(BrowsePaths.Length);
                foreach (var bp in BrowsePaths) bp.Encode(writer);
            }
        }
    }
}