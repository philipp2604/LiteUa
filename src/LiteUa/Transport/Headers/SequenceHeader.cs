using LiteUa.Encoding;

namespace LiteUa.Transport.Headers
{
    /// <summary>
    /// Represents the sequence header used in OPC UA communication.
    /// </summary>
    public class SequenceHeader
    {
        /// <summary>
        /// Gets or sets the sequence number of the message.
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Gets or sets the request identifier associated with the message.
        /// </summary>
        public uint RequestId { get; set; }

        /// <summary>
        /// Encodes the sequence header using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteUInt32(SequenceNumber);
            writer.WriteUInt32(RequestId);
        }
    }
}