using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Session
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class CloseSessionRequest
    {
        public static readonly NodeId NodeId = new(473);
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public bool DeleteSubscriptions { get; set; } = true;

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteBoolean(DeleteSubscriptions);
        }
    }
}
