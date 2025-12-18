using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method
    public class SetPublishingModeRequest
    {
        public static readonly NodeId NodeId = new(799);
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public bool PublishingEnabled { get; set; }
        public uint[]? SubscriptionIds { get; set; }

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