using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// <summary>
    /// Represents a DeleteMonitoredItemsRequest message in the OPC UA protocol.
    /// </summary>
    public class DeleteMonitoredItemsRequest
    {
        /// <summary>
        /// Gets the NodeId for the DeleteMonitoredItemsRequest message type.
        /// </summary>
        public static readonly NodeId NodeId = new(781);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> of the DeleteMonitoredItemsRequest.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the subscription ID associated with the DeleteMonitoredItemsRequest.
        /// </summary>
        public uint SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the monitored item IDs to be deleted.
        /// </summary>
        public uint[]? MonitoredItemIds { get; set; }

        /// <summary>
        /// Encodes the DeleteMonitoredItemsRequest using the provided <see cref="OpcUaBinaryWriter"/>. 
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteUInt32(SubscriptionId);

            if (MonitoredItemIds == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(MonitoredItemIds.Length);
                foreach (var id in MonitoredItemIds) writer.WriteUInt32(id);
            }
        }
    }
}