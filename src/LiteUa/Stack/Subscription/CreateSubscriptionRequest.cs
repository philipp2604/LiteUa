using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class CreateSubscriptionRequest
    {
        public static readonly NodeId NodeId = new(787);
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public double RequestedPublishingInterval { get; set; } = 1000.0;
        public uint RequestedLifetimeCount { get; set; } = 60;
        public uint RequestedMaxKeepAliveCount { get; set; } = 20;
        public uint MaxNotificationsPerPublish { get; set; } = 0;
        public bool PublishingEnabled { get; set; } = true;
        public byte Priority { get; set; } = 0;

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