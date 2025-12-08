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

    public class ReadRequest
    {
        public static readonly NodeId NodeId = new(631);

        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public double MaxAge { get; set; } = 0;
        public TimestampsToReturn TimestampsToReturn { get; set; } = TimestampsToReturn.Both;
        public ReadValueId[]? NodesToRead { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            writer.WriteDouble(MaxAge);
            writer.WriteUInt32((uint)TimestampsToReturn);

            // Array of ReadValueId
            if (NodesToRead == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(NodesToRead.Length);
                foreach (var node in NodesToRead) node.Encode(writer);
            }
        }
    }
}
