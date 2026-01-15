using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a CreateSubscriptionRequest message used to create a subscription in OPC UA.
    /// </summary>
    public class CreateSubscriptionRequest
    {
        /// <summary>
        /// Gets the NodeId for the CreateSubscriptionRequest message.
        /// </summary>
        public static readonly NodeId NodeId = new(787);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> for the CreateSubscriptionRequest message.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the requested publishing interval in milliseconds. Default is 1000 ms (1 second).
        /// </summary>
        public double RequestedPublishingInterval { get; set; } = 1000.0;

        /// <summary>
        /// Gets or sets the requested lifetime count. Default is 60.
        /// </summary>
        public uint RequestedLifetimeCount { get; set; } = 60;

        /// <summary>
        /// Gets or sets the requested maximum keep-alive count. Default is 20.
        /// </summary>
        public uint RequestedMaxKeepAliveCount { get; set; } = 20;

        /// <summary>
        /// Gets or sets the maximum notifications per publish. Default is 0 (no limit).
        /// </summary>
        public uint MaxNotificationsPerPublish { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether publishing is enabled. Default is true.
        /// </summary>
        public bool PublishingEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the priority of the subscription. Default is 0.
        /// </summary>
        public byte Priority { get; set; } = 0;

        /// <summary>
        /// Encodes the CreateSubscriptionRequest message using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteDouble(RequestedPublishingInterval);
            writer.WriteUInt32(RequestedLifetimeCount);
            writer.WriteUInt32(RequestedMaxKeepAliveCount);
            writer.WriteUInt32(MaxNotificationsPerPublish);
            writer.WriteBoolean(PublishingEnabled);
            writer.WriteByte(Priority);
        }
    }
}