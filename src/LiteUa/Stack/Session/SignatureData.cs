using LiteUa.Encoding;

namespace LiteUa.Stack.Session
{
    /// <summary>
    /// Represents signature data used in OPC UA messages.
    /// </summary>
    public class SignatureData
    {
        /// <summary>
        /// Gets or sets the algorithm used for the signature.
        /// </summary>
        public string? Algorithm { get; set; }

        /// <summary>
        /// Gets or sets the signature byte array.
        /// </summary>
        public byte[]? Signature { get; set; }

        /// <summary>
        /// Encodes the SignatureData into the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> used for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteString(Algorithm);
            writer.WriteByteString(Signature);
        }

        /// <summary>
        /// Gets a static instance of SignatureData representing a null signature.
        /// </summary>
        public static readonly SignatureData Null = new();
    }
}