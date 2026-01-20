using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a PublishResponse message in the OPC UA protocol.
    /// </summary>
    public class PublishResponse
    {
        /// <summary>
        /// Gets the NodeId for the PublishResponse message type.
        /// </summary>
        public static readonly NodeId NodeId = new(829);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> of the PublishResponse.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the subscription ID associated with the PublishResponse.
        /// </summary>
        public uint SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the available sequence numbers for the PublishResponse.
        /// </summary>
        public uint[]? AvailableSequenceNumbers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether there are more notifications.
        /// </summary>
        public bool MoreNotifications { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="NotificationMessage"/> of the PublishResponse.
        /// </summary>
        public NotificationMessage? NotificationMessage { get; set; }

        /// <summary>
        /// Gets or sets the results of the PublishResponse acknowledgments.
        /// </summary>
        public StatusCode[]? Results { get; set; }

        /// <summary>
        /// Gets or sets the diagnostic information for the PublishResponse.
        /// </summary>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes the PublishResponse using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);
            SubscriptionId = reader.ReadUInt32();

            int count = reader.ReadInt32();
            if (count > 0)
            {
                AvailableSequenceNumbers = new uint[count];
                for (int i = 0; i < count; i++) AvailableSequenceNumbers[i] = reader.ReadUInt32();
            }
            else
            {
                AvailableSequenceNumbers = [];
            }

            MoreNotifications = reader.ReadBoolean();
            NotificationMessage = NotificationMessage.Decode(reader);

            // Results (Acks results)
            int resCount = reader.ReadInt32();
            if (resCount > 0)
            {
                Results = new StatusCode[resCount];
                for (int i = 0; i < resCount; i++) Results[i] = StatusCode.Decode(reader);
            }

            if (reader.Position < reader.Length)
            {
                int diagCount = reader.ReadInt32();
                if (diagCount > 0)
                {
                    DiagnosticInfos = new DiagnosticInfo[diagCount];
                    for (int i = 0; i < diagCount; i++) DiagnosticInfos[i] = DiagnosticInfo.Decode(reader);
                }
            }
        }
    }
}