using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.View
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method
    public class TranslateBrowsePathsToNodeIdsRequest
    {
        public static readonly NodeId NodeId = new(554);
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public BrowsePath[]? BrowsePaths { get; set; }

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
