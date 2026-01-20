using LiteUa.Encoding;

namespace LiteUa.Transport.TcpMessages
{
    /// <summary>
    /// Represents a TCP Hello message used in the OPC UA TCP protocol handshake.
    /// </summary>
    /// <param name="endpointUrl">The endpoint URL of the server.</param>
    internal class TcpHelloMessage(string endpointUrl)
    {
        /// <summary>
        /// Gets the current protocol version supported by this implementation.
        /// </summary>
        public const uint CurrentProtocolVersion = 0x0;

        /// <summary>
        /// Gets or sets the protocol version.
        /// </summary>
        public uint ProtocolVersion { get; set; } = CurrentProtocolVersion;

        /// <summary>
        /// Gets or sets the size of the receive buffer.
        /// </summary>
        public uint ReceiveBufferSize { get; set; } = 0xFFFF;

        /// <summary>
        /// Gets or sets the size of the send buffer.
        /// </summary>
        public uint SendBufferSize { get; set; } = 0xFFFF;

        /// <summary>
        /// Gets or sets the maximum message size.
        /// </summary>
        public uint MaxMessageSize { get; set; } = 0; // No limit / server limit

        /// <summary>
        /// Gets or sets the maximum chunk count.
        /// </summary>
        public uint MaxChunkCount { get; set; } = 0; // No limit / server limit

        /// <summary>
        /// Gets or sets the endpoint URL of the server.
        /// </summary>
        public string EndpointUrl { get; set; } = endpointUrl ?? throw new ArgumentNullException(nameof(endpointUrl));

        /// <summary>
        /// Encodes the TCP Hello message using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
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