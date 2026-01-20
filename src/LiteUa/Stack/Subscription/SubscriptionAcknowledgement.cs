using LiteUa.Encoding;

namespace LiteUa.Stack.Subscription
{

    /// <summary>
    /// Represents a SubscriptionAcknowledgement in the OPC UA protocol.
    /// </summary>
    public class SubscriptionAcknowledgement
    {
        /// <summary>
        /// Gets or sets the SubscriptionId of the acknowledgement.
        /// </summary>
        public uint SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the SequenceNumber of the acknowledgement.
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Encodes the SubscriptionAcknowledgement using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteUInt32(SubscriptionId);
            writer.WriteUInt32(SequenceNumber);
        }
    }
}