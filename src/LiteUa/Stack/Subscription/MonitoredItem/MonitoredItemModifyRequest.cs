using LiteUa.Encoding;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// <summary>
    /// Represents a request to modify an existing monitored item in an OPC UA subscription.
    /// </summary>
    /// <param name="monitoredItemId">The monitored item id.</param>
    /// <param name="requestedParameters">The monitoring parameters.</param>
    public class MonitoredItemModifyRequest(uint monitoredItemId, MonitoringParameters requestedParameters)
    {
        /// <summary>
        /// Gets or sets the monitored item id.
        /// </summary>
        public uint MonitoredItemId { get; set; } = monitoredItemId;

        /// <summary>
        /// Gets or sets the monitoring parameters.
        /// </summary>
        public MonitoringParameters RequestedParameters { get; set; } = requestedParameters;

        /// <summary>
        /// Encodes the MonitoredItemModifyRequest using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteUInt32(MonitoredItemId);
            RequestedParameters.Encode(writer);
        }
    }
}