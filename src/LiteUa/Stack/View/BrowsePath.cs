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
    public class BrowsePath(NodeId startingNode, RelativePath relPath)
    {
        public NodeId StartingNode { get; set; } = startingNode;
        public RelativePath RelativePath { get; set; } = relPath;

        public void Encode(OpcUaBinaryWriter writer)
        {
            StartingNode.Encode(writer);
            RelativePath.Encode(writer);
        }
    }
}
