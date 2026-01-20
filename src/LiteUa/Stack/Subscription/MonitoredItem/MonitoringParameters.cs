using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// <summary>
    /// Represents the monitoring parameters for a monitored item in an OPC UA subscription.
    /// </summary>
    public class MonitoringParameters
    {
        /// <summary>
        /// Gets or sets the client handle associated with the monitored item.
        /// </summary>
        public uint ClientHandle { get; set; }

        /// <summary>
        /// Gets or sets the sampling interval for the monitored item.
        /// </summary>
        public double SamplingInterval { get; set; } = -1; // Default

        /// <summary>
        /// Gets or sets the filter to be applied to the monitored item. Null for DataChange.
        /// </summary>
        public ExtensionObject? Filter { get; set; } // Null for DataChange

        /// <summary>
        /// Gets or sets the queue size for the monitored item.
        /// </summary>
        public uint QueueSize { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether to discard the oldest data when the queue is full.
        /// </summary>
        public bool DiscardOldest { get; set; } = true;

        /// <summary>
        /// Encodes the monitoring parameters using the specified <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteUInt32(ClientHandle);
            writer.WriteDouble(SamplingInterval);
            // Filter
            if (Filter == null) new ExtensionObject().Encode(writer); // Null/Empty
            else Filter.Encode(writer);

            writer.WriteUInt32(QueueSize);
            writer.WriteBoolean(DiscardOldest);
        }
    }
}