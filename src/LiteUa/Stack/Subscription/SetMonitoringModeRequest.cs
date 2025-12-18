using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method
    public class SetMonitoringModeRequest
    {
        public static readonly NodeId NodeId = new(769);
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public uint SubscriptionId { get; set; }
        public uint MonitoringMode { get; set; } // 0=Disabled, 1=Sampling, 2=Reporting
        public uint[]? MonitoredItemIds { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteUInt32(SubscriptionId);
            writer.WriteUInt32(MonitoringMode);

            if (MonitoredItemIds == null)
            {
                writer.WriteInt32(-1);
            }
            else
            {
                writer.WriteInt32(MonitoredItemIds.Length);
                foreach (var id in MonitoredItemIds) writer.WriteUInt32(id);
            }
        }
    }
}