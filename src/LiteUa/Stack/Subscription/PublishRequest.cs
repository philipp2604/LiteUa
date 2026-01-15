using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a PublishRequest message used to request notifications from a subscription in OPC UA.
    /// </summary>
    public class PublishRequest
    {
        /// <summary>
        /// Gets the NodeId for the PublishRequest message.
        /// </summary>
        public static readonly NodeId NodeId = new(826);

        /// <summary>
        /// Gets the <see cref="RequestHeader"/> for the PublishRequest message.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the array of <see cref="SubscriptionAcknowledgement"/> for the PublishRequest message.
        /// </summary>
        public SubscriptionAcknowledgement[] SubscriptionAcknowledgements { get; set; } = [];

        /// <summary>
        /// Encodes the PublishRequest message using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            if (SubscriptionAcknowledgements == null)
            {
                writer.WriteInt32(-1);
            }
            else
            {
                writer.WriteInt32(SubscriptionAcknowledgements.Length);
                foreach (var ack in SubscriptionAcknowledgements)
                {
                    ack.Encode(writer);
                }
            }
        }
    }
}