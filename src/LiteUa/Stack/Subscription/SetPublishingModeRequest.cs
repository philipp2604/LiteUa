using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a SetPublishingModeRequest message in the OPC UA protocol.
    /// </summary>
    public class SetPublishingModeRequest
    {
        /// <summary>
        /// Gets the NodeId for the SetPublishingModeRequest message type.
        /// </summary>
        public static readonly NodeId NodeId = new(799);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> of the SetPublishingModeRequest.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets a value indicating whether publishing shall be enabled.
        /// </summary>
        public bool PublishingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the subscription IDs to which the publishing mode will be applied.
        /// </summary>
        public uint[]? SubscriptionIds { get; set; }

        /// <summary>
        /// Encodies the SetPublishingModeRequest using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteBoolean(PublishingEnabled);

            if (SubscriptionIds == null)
            {
                writer.WriteInt32(-1);
            }
            else
            {
                writer.WriteInt32(SubscriptionIds.Length);
                foreach (var id in SubscriptionIds) writer.WriteUInt32(id);
            }
        }
    }
}