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

    public class PublishRequest
    {
        public static readonly NodeId NodeId = new(826);

        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public SubscriptionAcknowledgement[] SubscriptionAcknowledgements { get; set; } = [];

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            if (SubscriptionAcknowledgements == null)
            {
                writer.WriteInt32(-1);
            }
            else
            {
                writer.WriteInt32(SubscriptionAcknowledgements.Length);
                foreach (var ack in SubscriptionAcknowledgements)
                {
                    ack.Encode(writer);
                }
            }
        }
    }
}
