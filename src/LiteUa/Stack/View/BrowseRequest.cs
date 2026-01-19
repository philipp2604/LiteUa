using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.View
{ 
    /// <summary>
    /// Represents a request to browse the references of one or more nodes in an OPC UA server address space.
    /// </summary>
    public class BrowseRequest
    {

        /// TODO: Implement ViewDescription

        /// <summary>
        /// Gets the NodeId for the BrowseRequest type.
        /// </summary>
        public static readonly NodeId NodeId = new(527);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> for the operation.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the description of the view to use for the operation.
        /// </summary>
        public ViewDescription? View { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of references to return per node in a response.
        /// </summary>
        /// <remarks>Set this property to 0 to indicate that there is no limit on the number of references
        /// returned per node.</remarks>
        public uint RequestedMaxReferencesPerNode { get; set; } = 0; // 0 = unlimited

        /// <summary>
        /// Gets or sets the collection of node browse descriptions to use for the browse operation.
        /// </summary>
        /// <remarks>Each element in the array specifies a node and the parameters for browsing its
        /// references. If the array is null or empty, no nodes will be browsed.</remarks>
        public BrowseDescription[]? NodesToBrowse { get; set; }

        /// <summary>
        /// Encodes the current object using the specified <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding. Cannot be null.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            // ViewDescription (Null = empty): NodeId(0), DateTime(0), UInt32(0)
            // ViewDescription: NodeId(ViewId), DateTime(Timestamp), UInt32(ViewVersion)

            new NodeId(0).Encode(writer);
            writer.WriteDateTime(DateTime.MinValue);
            writer.WriteUInt32(0);

            writer.WriteUInt32(RequestedMaxReferencesPerNode);

            if (NodesToBrowse == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(NodesToBrowse.Length);
                foreach (var node in NodesToBrowse) node.Encode(writer);
            }
        }
    }
}