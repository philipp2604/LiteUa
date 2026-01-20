using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// <summary>
    /// Represents the result of creating a monitored item in an OPC UA subscription.
    /// </summary>
    public class MonitoredItemCreateResult
    {
        /// <summary>
        /// Gets or sets the status code indicating the result of the monitored item creation.
        /// </summary>
        public StatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the monitored item.
        /// </summary>
        public uint MonitoredItemId { get; set; }

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
        /// Decodes a <see cref="MonitoredItemCreateResult"/> using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded instance of <see cref="MonitoredItemCreateResult"/>.</returns>
        public static MonitoredItemCreateResult Decode(OpcUaBinaryReader reader)
        {
            return new MonitoredItemCreateResult
            {
                StatusCode = StatusCode.Decode(reader),
                MonitoredItemId = reader.ReadUInt32(),
                RevisedSamplingInterval = reader.ReadDouble(),
                RevisedQueueSize = reader.ReadUInt32(),
                FilterResult = ExtensionObject.Decode(reader)
            };
        }
    }
}