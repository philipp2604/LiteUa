using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a DeleteSubscriptionsRequest message used to delete subscriptions in OPC UA.
    /// </summary>
    public class DeleteSubscriptionsRequest
    {
        /// <summary>
        /// Gets the NodeId for the DeleteSubscriptionsRequest message.
        /// </summary>
        public static readonly NodeId NodeId = new(847);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> for the DeleteSubscriptionsRequest message.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the array of subscription IDs to be deleted.
        /// </summary>
        public uint[]? SubscriptionIds { get; set; }

        /// <summary>
        /// Encodes the DeleteSubscriptionsRequest message into the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            if (SubscriptionIds == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(SubscriptionIds.Length);
                foreach (var id in SubscriptionIds) writer.WriteUInt32(id);
            }
        }
    }
}