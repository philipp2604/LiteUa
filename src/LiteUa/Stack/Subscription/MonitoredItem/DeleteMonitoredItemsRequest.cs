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

    public class DeleteMonitoredItemsRequest
    {
        public static readonly NodeId NodeId = new(781);
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public uint SubscriptionId { get; set; }
        public uint[]? MonitoredItemIds { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteUInt32(SubscriptionId);

            if (MonitoredItemIds == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(MonitoredItemIds.Length);
                foreach (var id in MonitoredItemIds) writer.WriteUInt32(id);
            }
        }
    }
}
