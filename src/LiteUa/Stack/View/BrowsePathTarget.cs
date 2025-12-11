using LiteUa.BuiltIn;
using LiteUa.Encoding;
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
    public class BrowsePathTarget
    {
        public ExpandedNodeId? TargetId { get; set; }
        public uint RemainingPathIndex { get; set; }

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
