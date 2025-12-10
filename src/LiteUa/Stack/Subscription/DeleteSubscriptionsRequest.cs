using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Subscription
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class DeleteSubscriptionsRequest
    {
        public static readonly NodeId NodeId = new(793);
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public uint[]? SubscriptionIds { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            if (SubscriptionIds == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(SubscriptionIds.Length);
                foreach (var id in SubscriptionIds) writer.WriteUInt32(id);
            }
        }
    }
}
