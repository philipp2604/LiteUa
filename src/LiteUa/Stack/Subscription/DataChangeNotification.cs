using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Subscription.MonitoredItem;

namespace LiteUa.Stack.Subscription
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class DataChangeNotification
    {
        public MonitoredItemNotification?[]? MonitoredItems { get; set; }
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        public static DataChangeNotification Decode(OpcUaBinaryReader reader)
        {
            var dcn = new DataChangeNotification();

            int count = reader.ReadInt32();
            if (count > 0)
            {
                dcn.MonitoredItems = new MonitoredItemNotification[count];
                for (int i = 0; i < count; i++) dcn.MonitoredItems[i] = MonitoredItemNotification.Decode(reader);
            }

            int diagCount = reader.ReadInt32();
            if (diagCount > 0)
            {
                dcn.DiagnosticInfos = new DiagnosticInfo[diagCount];
                for (int i = 0; i < diagCount; i++) dcn.DiagnosticInfos[i] = DiagnosticInfo.Decode(reader);
            }

            return dcn;
        }
    }
}