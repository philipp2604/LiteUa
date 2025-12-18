using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

/// TODO: Add unit tests
/// TODO: fix documentation comments
/// TODO: Add ToString() method

namespace LiteUa.Stack.View
{
    public class BrowseNextRequest
    {
        public static readonly NodeId NodeId = new(533);
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public bool ReleaseContinuationPoints { get; set; } = false;
        public byte[][]? ContinuationPoints { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteBoolean(ReleaseContinuationPoints);

            if (ContinuationPoints == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(ContinuationPoints.Length);
                foreach (var cp in ContinuationPoints) writer.WriteByteString(cp);
            }
        }
    }
}