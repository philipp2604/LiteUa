using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
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

    public class CallRequest(CallMethodRequest[] methodsToCall)
    {
        public static readonly NodeId NodeId = new(712);

        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public CallMethodRequest[] MethodsToCall { get; set; } = methodsToCall;

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            if (MethodsToCall == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(MethodsToCall.Length);
                foreach (var m in MethodsToCall) m.Encode(writer);
            }
        }
    }
}
