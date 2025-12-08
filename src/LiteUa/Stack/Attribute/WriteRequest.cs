using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
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

    public class WriteRequest
    {
        public static readonly NodeId NodeId = new(673);

        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public WriteValue[]? NodesToWrite { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            if (NodesToWrite == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(NodesToWrite.Length);
                foreach (var node in NodesToWrite) node.Encode(writer);
            }
        }
    }
}
