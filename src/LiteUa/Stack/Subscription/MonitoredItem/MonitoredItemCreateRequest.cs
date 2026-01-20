using LiteUa.Encoding;
using LiteUa.Stack.Attribute;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// <summary>
    /// Represents a request to create a monitored item in the OPC UA protocol.
    /// </summary>
    /// <param name="itemToMonitor">The item to monitor.</param>
    /// <param name="monitoringMode">The monitoring mode</param>
    /// <param name="requestedParameters">The monitoring parameters.</param>

    public class MonitoredItemCreateRequest(ReadValueId itemToMonitor, uint monitoringMode, MonitoringParameters requestedParameters)
    {
        /// <summary>
        /// Gets or sets the item to monitor.
        /// </summary>
        public ReadValueId ItemToMonitor { get; set; } = itemToMonitor;

        /// <summary>
        /// Gets or sets the monitoring mode.
        /// </summary>
        public uint MonitoringMode { get; set; } = monitoringMode;

        /// <summary>
        /// Gets or sets the requested monitoring parameters.
        /// </summary>
        public MonitoringParameters RequestedParameters { get; set; } = requestedParameters;

        /// <summary>
        /// Encodes the MonitoredItemCreateRequest using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            ItemToMonitor.Encode(writer);
            writer.WriteUInt32(MonitoringMode);
            RequestedParameters.Encode(writer);
        }
    }
}