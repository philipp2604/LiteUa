using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a SetMonitoringModeRequest message in the OPC UA protocol.
    /// </summary>
    public class SetMonitoringModeRequest
    {
        /// <summary>
        /// Gets the NodeId for the SetMonitoringModeRequest message type.
        /// </summary>
        public static readonly NodeId NodeId = new(769);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> of the SetMonitoringModeRequest.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the subscription ID associated with the SetMonitoringModeRequest.
        /// </summary>
        public uint SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the monitoring mode to be applied. 0=Disabled, 1=Sampling, 2=Reporting.
        /// </summary>
        public uint MonitoringMode { get; set; } // 0=Disabled, 1=Sampling, 2=Reporting

        /// <summary>
        /// Gets or sets the monitored item IDs to which the monitoring mode will be applied.
        /// </summary>
        public uint[]? MonitoredItemIds { get; set; }

        /// <summary>
        /// Encodes the SetMonitoringModeRequest using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteUInt32(SubscriptionId);
            writer.WriteUInt32(MonitoringMode);

            if (MonitoredItemIds == null)
            {
                writer.WriteInt32(-1);
            }
            else
            {
                writer.WriteInt32(MonitoredItemIds.Length);
                foreach (var id in MonitoredItemIds) writer.WriteUInt32(id);
            }
        }
    }
}