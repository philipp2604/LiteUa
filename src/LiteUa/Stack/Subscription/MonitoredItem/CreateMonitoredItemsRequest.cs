using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// <summary>
    /// Represents a CreateMonitoredItemsRequest message in the OPC UA protocol.
    /// </summary>
    public class CreateMonitoredItemsRequest
    {
        /// <summary>
        /// Gets the NodeId for the CreateMonitoredItemsRequest message type.
        /// </summary>
        public static readonly NodeId NodeId = new(751);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> of the CreateMonitoredItemsRequest.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the subscription ID associated with the CreateMonitoredItemsRequest.
        /// </summary>
        public uint SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the timestamps to return for the monitored items, default is 2 = Both.
        /// </summary>
        public TimestampsToReturn TimestampsToReturn { get; set; } = TimestampsToReturn.Both; // Both

        /// <summary>
        /// Gets or sets the monitored items to be created.
        /// </summary>
        public MonitoredItemCreateRequest[]? ItemsToCreate { get; set; }

        /// <summary>
        /// Encodes the CreateMonitoredItemsRequest using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteUInt32(SubscriptionId);
            writer.WriteUInt32((uint)TimestampsToReturn);

            if (ItemsToCreate == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(ItemsToCreate.Length);
                foreach (var item in ItemsToCreate) item.Encode(writer);
            }
        }
    }
}