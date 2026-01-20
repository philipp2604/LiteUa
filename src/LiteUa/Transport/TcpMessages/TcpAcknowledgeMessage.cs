using LiteUa.Encoding;

namespace LiteUa.Transport.TcpMessages
{
    /// <summary>
    /// Represents a TCP Acknowledge message used in the OPC UA TCP protocol.
    /// </summary>
    internal class TcpAcknowledgeMessage
    {
        /// <summary>
        /// Gets the protocol version supported by the server.
        /// </summary>
        public uint ProtocolVersion { get; private set; }

        /// <summary>
        /// Gets the size of the receive buffer.
        /// </summary>
        public uint ReceiveBufferSize { get; private set; }

        /// <summary>
        /// Gets the size of the send buffer.
        /// </summary>
        public uint SendBufferSize { get; private set; }

        /// <summary>
        /// Gets the maximum message size supported.
        /// </summary>
        public uint MaxMessageSize { get; private set; }

        /// <summary>
        /// Gets the maximum number of chunks supported.
        /// </summary>
        public uint MaxChunkCount { get; private set; }

        /// <summary>
        /// Decodes the TCP Acknowledge message using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader"></param>
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