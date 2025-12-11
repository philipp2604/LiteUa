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
    public class RelativePathElement
    {
        public NodeId ReferenceTypeId { get; set; } = new NodeId(33); // HierarchicalReferences
        public bool IsInverse { get; set; } = false;
        public bool IncludeSubtypes { get; set; } = true;
        public QualifiedName? TargetName { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            ReferenceTypeId.Encode(writer);
            writer.WriteBoolean(IsInverse);
            writer.WriteBoolean(IncludeSubtypes);
            TargetName?.Encode(writer);
        }
    }
}
