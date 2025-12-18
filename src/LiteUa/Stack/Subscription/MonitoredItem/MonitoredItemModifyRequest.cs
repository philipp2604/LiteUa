using LiteUa.Encoding;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method
    public class MonitoredItemModifyRequest(uint monitoredItemId, MonitoringParameters requestedParameters)
    {
        public uint MonitoredItemId { get; set; } = monitoredItemId;
        public MonitoringParameters RequestedParameters { get; set; } = requestedParameters;

        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteUInt32(MonitoredItemId);
            RequestedParameters.Encode(writer);
        }
    }
}