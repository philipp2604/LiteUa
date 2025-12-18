using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class CreateSubscriptionResponse
    {
        public static readonly NodeId NodeId = new(790);

        public ResponseHeader? ResponseHeader { get; set; }
        public uint SubscriptionId { get; set; }
        public double RevisedPublishingInterval { get; set; }
        public uint RevisedLifetimeCount { get; set; }
        public uint RevisedMaxKeepAliveCount { get; set; }
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);
            SubscriptionId = reader.ReadUInt32();
            RevisedPublishingInterval = reader.ReadDouble();
            RevisedLifetimeCount = reader.ReadUInt32();
            RevisedMaxKeepAliveCount = reader.ReadUInt32();

            if (reader.Position < reader.Length)
            {
                int count = reader.ReadInt32();
                if (count > 0)
                {
                    DiagnosticInfos = new DiagnosticInfo[count];
                    for (int i = 0; i < count; i++) DiagnosticInfos[i] = DiagnosticInfo.Decode(reader);
                }
            }
        }
    }
}