using LiteUa.Encoding;

namespace LiteUa.Transport.TcpMessages
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    internal class TcpHelloMessage(string endpointUrl)
    {
        public const uint CurrentProtocolVersion = 0x0;
        public uint ProtocolVersion { get; set; } = CurrentProtocolVersion;
        public uint ReceiveBufferSize { get; set; } = 0xFFFF;
        public uint SendBufferSize { get; set; } = 0xFFFF;
        public uint MaxMessageSize { get; set; } = 0; // No limit / server limit
        public uint MaxChunkCount { get; set; } = 0; // No limit / server limit
        public string EndpointUrl { get; set; } = endpointUrl ?? throw new ArgumentNullException(nameof(endpointUrl));

        public void Encode(OpcUaBinaryWriter writer)
        {
            long startPos = writer.Position;

            // header
            writer.WriteBytes("HEL"u8.ToArray());
            writer.WriteByte((byte)'F');
            writer.WriteUInt32(0); // Message size placeholder

            // body
            writer.WriteUInt32(ProtocolVersion);
            writer.WriteUInt32(ReceiveBufferSize);
            writer.WriteUInt32(SendBufferSize);
            writer.WriteUInt32(MaxMessageSize);
            writer.WriteUInt32(MaxChunkCount);
            writer.WriteString(EndpointUrl);

            // length patching
            long endPos = writer.Position;
            uint totalLength = (uint)(endPos - startPos);
            writer.Seek((startPos + 4), SeekOrigin.Begin);
            writer.WriteUInt32(totalLength);
            writer.Seek(endPos, SeekOrigin.Begin);
        }
    }
}