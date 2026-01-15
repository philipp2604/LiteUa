using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// <summary>
    /// Represents the result of modifying a monitored item in an OPC UA subscription.
    /// </summary>
    public class MonitoredItemModifyResult
    {
        /// <summary>
        /// Gets or sets the status code indicating the result of the monitored item modification.
        /// </summary>
        public StatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the revised sampling interval for the monitored item.
        /// </summary>
        public double RevisedSamplingInterval { get; set; }

        /// <summary>
        /// Gets or sets the revised queue size for the monitored item.
        /// </summary>
        public uint RevisedQueueSize { get; set; }

        /// <summary>
        /// Gets or sets the filter result associated with the monitored item.
        /// </summary>
        public ExtensionObject? FilterResult { get; set; }

        /// <summary>
        /// Decodes a <see cref="MonitoredItemModifyResult"/> using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded instance of <see cref="MonitoredItemModifyResult"/>.</returns>
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