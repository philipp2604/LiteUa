using LiteUa.Encoding;

namespace LiteUa.Transport.Headers
{
    /// <summary>
    /// Represents the header for a secure conversation message in OPC UA.
    /// </summary>
    public class SecureConversationMessageHeader
    {
        /// <summary>
        /// Gets or sets the message type, which is a 3-character string indicating the type of message (e.g., "MSG" for regular messages, "OPN" for open secure channel, "CLO" for close secure channel).
        /// </summary>
        public string? MessageType { get; set; }

        /// <summary>
        /// Gets or sets the chunk type, which is a single character indicating the chunk type of the message (e.g., 'C' for intermediate chunk, 'F' for final chunk, 'A' for abort).
        /// </summary>
        public char ChunkType { get; set; }

        /// <summary>
        /// Gets or sets the size of the message in bytes, including the header.
        /// </summary>
        public uint MessageSize { get; set; }

        /// <summary>
        /// Gets or sets the secure channel identifier associated with the message.
        /// </summary>
        public uint SecureChannelId { get; set; }

        /// <summary>
        /// Encodes the secure conversation message header using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Encode(OpcUaBinaryWriter writer)
        {
            if (MessageType == null || MessageType.Length != 3) throw new ArgumentException("MessageType must be 3 chars");

            writer.WriteByte((byte)MessageType[0]);
            writer.WriteByte((byte)MessageType[1]);
            writer.WriteByte((byte)MessageType[2]);
            writer.WriteByte((byte)ChunkType);

            writer.WriteUInt32(MessageSize);
            writer.WriteUInt32(SecureChannelId);
        }
    }
}