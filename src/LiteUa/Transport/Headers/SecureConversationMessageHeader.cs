using LiteUa.Encoding;

namespace LiteUa.Transport.Headers
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class SecureConversationMessageHeader
    {
        // "MSG", "OPN", "CLO"
        public string? MessageType { get; set; }

        // 'C' (Intermediate Chunk), 'F' (Final Chunk), 'A' (Abort)
        public char ChunkType { get; set; }

        public uint MessageSize { get; set; }
        public uint SecureChannelId { get; set; }

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