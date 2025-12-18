using LiteUa.Encoding;

namespace LiteUa.BuiltIn
{
    /// TODO: Add unit tests

    /// <summary>
    /// Represents a StatusCode in OPC UA, indicating the result of an operation.
    /// </summary>
    /// <remarks>
    /// Creates a new instance of StatusCode with the specified code.
    /// </remarks>
    /// <param name="code"></param>
    public struct StatusCode(uint code)
    {
        /// <summary>
        /// Gets or sets the numeric code of the StatusCode.
        /// </summary>
        public uint Code { get; set; } = code;

        /// <summary>
        /// Gets a value indicating whether the StatusCode represents a good status.
        /// </summary>
        public readonly bool IsGood => (Code & 0x80000000) == 0;

        /// <summary>
        /// Gets a value indicating whether the StatusCode represents a bad status.
        /// </summary>
        public readonly bool IsBad => (Code & 0x80000000) != 0;

        /// <summary>
        /// Decodes a StatusCode using the provided <see cref="OpcUaBinaryReader">.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded instance of <see cref="StatusCode"/>.</returns>
        public static StatusCode Decode(OpcUaBinaryReader reader)
        {
            return new StatusCode(reader.ReadUInt32());
        }

        /// <summary>
        /// Encodes a StatusCode using the provided <see cref="OpcUaBinaryWriter">.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for decoding.</param>
        public readonly void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteUInt32(Code);
        }

        public override readonly string ToString() => $"0x{Code:X8}";
    }
}