using LiteUa.BuiltIn;
using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Method
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class CallMethodRequest(NodeId objectId, NodeId methodId, Variant[]? inputArguments = null)
    {
        public static readonly NodeId NodeId = new(706);

        public NodeId ObjectId { get; set; } = objectId;
        public NodeId MethodId { get; set; } = methodId;
        public Variant[]? InputArguments { get; set; } = inputArguments;

        public void Encode(OpcUaBinaryWriter writer)
        {
            ObjectId.Encode(writer);
            MethodId.Encode(writer);

            if (InputArguments == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(InputArguments.Length);
                foreach (var arg in InputArguments) arg.Encode(writer);
            }
        }
    }
}
