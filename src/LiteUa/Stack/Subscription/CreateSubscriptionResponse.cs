using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a CreateSubscriptionResponse message used to respond to a CreateSubscriptionRequest in OPC UA.
    /// </summary>
    public class CreateSubscriptionResponse
    {
        /// <summary>
        /// Gets the NodeId for the CreateSubscriptionResponse message.
        /// </summary>
        public static readonly NodeId NodeId = new(790);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> for the CreateSubscriptionResponse message.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the SubscriptionId assigned by the server.
        /// </summary>
        public uint SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the revised publishing interval in milliseconds.
        /// </summary>
        public double RevisedPublishingInterval { get; set; }

        /// <summary>
        /// Gets or sets the revised lifetime count.
        /// </summary>
        public uint RevisedLifetimeCount { get; set; }

        /// <summary>
        /// Gets or sets the revised maximum keep-alive count.
        /// </summary>
        public uint RevisedMaxKeepAliveCount { get; set; }

        /// <summary>
        /// Gets or sets the array of <see cref="DiagnosticInfo"/> for the CreateSubscriptionResponse.
        /// </summary>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes a CreateSubscriptionResponse message using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
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