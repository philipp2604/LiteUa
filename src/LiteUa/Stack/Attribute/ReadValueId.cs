using LiteUa.BuiltIn;
using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Attribute
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class ReadValueId(NodeId nodeId)
    {
        public NodeId NodeId { get; set; } = nodeId;
        public uint AttributeId { get; set; } = 13; // Value
        public string? IndexRange { get; set; } // null
        public QualifiedName? DataEncoding { get; set; } // null

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            writer.WriteUInt32(AttributeId);
            writer.WriteString(IndexRange);

            if (DataEncoding == null)
            {
                writer.WriteUInt16(0); // NS 0
                writer.WriteString(null); // Name null
            }
            else
            {
                DataEncoding.Encode(writer);
            }
        }
    }
}
