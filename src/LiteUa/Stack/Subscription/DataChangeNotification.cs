using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Subscription.MonitoredItem;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a DataChangeNotification in OPC UA.
    /// </summary>
    public class DataChangeNotification
    {
        /// <summary>
        /// Gets or sets the array of MonitoredItemNotifications.
        /// </summary>
        public MonitoredItemNotification?[]? MonitoredItems { get; set; }

        /// <summary>
        /// Gets or sets the array of DiagnosticInfos.
        /// </summary>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes a DataChangeNotification from the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded <see cref="DataChangeNotification"/> instance.</returns>
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