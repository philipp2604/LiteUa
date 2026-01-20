using LiteUa.Encoding;

namespace LiteUa.Transport.TcpMessages
{
    /// <summary>
    /// Represents a TCP error message in the OPC UA protocol.
    /// </summary>
    internal class TcpErrorMessage
    {
        /// <summary>
        /// Gets the error code associated with the TCP error message.
        /// </summary>
        public uint ErrorCode { get; private set; }

        /// <summary>
        /// Gets the reason for the TCP error message.
        /// </summary>
        public string? Reason { get; private set; }

        /// <summary>
        /// Decodes the TCP error message using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ErrorCode = reader.ReadUInt32();
            Reason = reader.ReadString();
        }
    }
}