using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method
    public class MonitoredItemModifyResult
    {
        public StatusCode StatusCode { get; set; }
        public double RevisedSamplingInterval { get; set; }
        public uint RevisedQueueSize { get; set; }
        public ExtensionObject? FilterResult { get; set; }

        public static MonitoredItemModifyResult Decode(OpcUaBinaryReader reader)
        {
            return new MonitoredItemModifyResult
            {
                StatusCode = StatusCode.Decode(reader),
                RevisedSamplingInterval = reader.ReadDouble(),
                RevisedQueueSize = reader.ReadUInt32(),
                FilterResult = ExtensionObject.Decode(reader)
            };
        }
    }
}