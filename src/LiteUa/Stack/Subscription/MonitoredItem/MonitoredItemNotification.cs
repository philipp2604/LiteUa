using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// <summary>
    /// Represents a notification for a monitored item in an OPC UA subscription.
    /// </summary>
    public class MonitoredItemNotification
    {
        /// <summary>
        /// Gets or sets the client handle associated with the monitored item.
        /// </summary>
        public uint ClientHandle { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataValue"/> of the monitored item.
        /// </summary>
        public DataValue? Value { get; set; }

        /// <summary>
        /// Decodes a <see cref="MonitoredItemNotification"/> using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded instance of <see cref="MonitoredItemNotification"/>.</returns>
        public static MonitoredItemNotification Decode(OpcUaBinaryReader reader)
        {
            return new MonitoredItemNotification
            {
                ClientHandle = reader.ReadUInt32(),
                Value = DataValue.Decode(reader)
            };
        }
    }
}