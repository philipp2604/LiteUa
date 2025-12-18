using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method
    public class ModifyMonitoredItemsRequest
    {
        public static readonly NodeId NodeId = new(763);
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public uint SubscriptionId { get; set; }
        public uint TimestampsToReturn { get; set; } = 2;
        public MonitoredItemModifyRequest[]? ItemsToModify { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteUInt32(SubscriptionId);
            writer.WriteUInt32(TimestampsToReturn);

            if (ItemsToModify == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(ItemsToModify.Length);
                foreach (var i in ItemsToModify) i.Encode(writer);
            }
        }
    }
}