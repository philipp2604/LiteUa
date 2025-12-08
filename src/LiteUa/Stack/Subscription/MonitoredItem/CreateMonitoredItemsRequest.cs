using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class CreateMonitoredItemsRequest
    {
        public static readonly NodeId NodeId = new(751);
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public uint SubscriptionId { get; set; }
        public uint TimestampsToReturn { get; set; } = 2; // Both
        public MonitoredItemCreateRequest[]? ItemsToCreate { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteUInt32(SubscriptionId);
            writer.WriteUInt32(TimestampsToReturn);

            if (ItemsToCreate == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(ItemsToCreate.Length);
                foreach (var item in ItemsToCreate) item.Encode(writer);
            }
        }
    }
}
