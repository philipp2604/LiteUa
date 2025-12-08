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

    public class BrowseDescription(NodeId nodeId)
    {
        public NodeId NodeId { get; set; } = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        public BrowseDirection BrowseDirection { get; set; } = BrowseDirection.Forward;
        public NodeId ReferenceTypeId { get; set; } = new NodeId(33); // HierarchicalReferences
        public bool IncludeSubtypes { get; set; } = true;
        public uint NodeClassMask { get; set; } = 0; // 0 = All
        public uint ResultMask { get; set; } = 63; // All

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            writer.WriteInt32((int)BrowseDirection);
            ReferenceTypeId.Encode(writer);
            writer.WriteBoolean(IncludeSubtypes);
            writer.WriteUInt32(NodeClassMask);
            writer.WriteUInt32(ResultMask);
        }
    }
}
