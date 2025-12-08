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

    public class ReferenceDescription
    {
        public NodeId? ReferenceTypeId { get; set; }
        public bool IsForward { get; set; }
        public ExpandedNodeId? NodeId { get; set; }
        public QualifiedName? BrowseName { get; set; }
        public LocalizedText? DisplayName { get; set; }
        public uint NodeClass { get; set; } // Enum NodeClass
        public ExpandedNodeId? TypeDefinition { get; set; }

        public static ReferenceDescription Decode(OpcUaBinaryReader reader)
        {
            return new ReferenceDescription
            {
                ReferenceTypeId = BuiltIn.NodeId.Decode(reader),
                IsForward = reader.ReadBoolean(),
                NodeId = ExpandedNodeId.Decode(reader),
                BrowseName = QualifiedName.Decode(reader),
                DisplayName = LocalizedText.Decode(reader),
                NodeClass = reader.ReadUInt32(),
                TypeDefinition = ExpandedNodeId.Decode(reader)
            };
        }
    }
}
