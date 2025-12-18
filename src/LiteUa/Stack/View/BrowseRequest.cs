using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.View
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method
    /// TODO: Implement ViewDescription

    public class BrowseRequest
    {
        public static readonly NodeId NodeId = new(527);

        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public ViewDescription? View { get; set; } // Nullable
        public uint RequestedMaxReferencesPerNode { get; set; } = 0; // 0 = unlimited
        public BrowseDescription[]? NodesToBrowse { get; set; }

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