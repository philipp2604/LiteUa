using LiteUa.Encoding;

namespace LiteUa.Transport.TcpMessages
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    internal class TcpAcknowledgeMessage
    {
        public uint ProtocolVersion { get; private set; }
        public uint ReceiveBufferSize { get; private set; }
        public uint SendBufferSize { get; private set; }
        public uint MaxMessageSize { get; private set; }
        public uint MaxChunkCount { get; private set; }

        public void Decode(OpcUaBinaryReader reader)
        {
            ProtocolVersion = reader.ReadUInt32();
            ReceiveBufferSize = reader.ReadUInt32();
            SendBufferSize = reader.ReadUInt32();
            MaxMessageSize = reader.ReadUInt32();
            MaxChunkCount = reader.ReadUInt32();
        }
    }
}