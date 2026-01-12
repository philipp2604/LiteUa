using LiteUa.Encoding;

namespace LiteUa.BuiltIn
{
    /// <summary>
    /// Represents diagnostic information in OPC UA, providing details about errors or issues.
    /// </summary>
    public class DiagnosticInfo
    {
        /// <summary>
        /// Gets or sets the SymbolicId of the DiagnosticInfo.
        /// </summary>
        public int? SymbolicId { get; set; }

        /// <summary>
        /// Gets or sets the NamespaceUri of the DiagnosticInfo.
        /// </summary>
        public int? NamespaceUri { get; set; }

        /// <summary>
        /// Gets or sets the readable summary of the DiagnosticInfo.
        /// </summary>
        public int? LocalizedText { get; set; }

        /// <summary>
        /// Gets or sets the Locale of the DiagnosticInfo.
        /// </summary>
        public int? Locale { get; set; }

        /// <summary>
        /// Gets or sets the specific diagnostic information of the DiagnosticInfo.
        /// </summary>
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// Gets or sets the InnerStatusCode of the DiagnosticInfo.
        /// </summary>
        public uint? InnerStatusCode { get; set; }

        /// <summary>
        /// Gets or sets the InnerDiagnosticInfo of the DiagnosticInfo.
        /// </summary>
        public DiagnosticInfo? InnerDiagnosticInfo { get; set; }

        /// <summary>
        /// Decodes a DiagnosticInfo object using the provided OpcUaBinaryReader.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded instance of <see cref="DiagnosticInfo""/>.</returns>
        public static DiagnosticInfo? Decode(OpcUaBinaryReader reader)
        {
            byte mask = reader.ReadByte();

            // 0x00: No DiagnosticInfo present, end of recursion
            if (mask == 0) return null;

            var info = new DiagnosticInfo();

            if ((mask & 0x01) != 0) info.SymbolicId = reader.ReadInt32();
            if ((mask & 0x02) != 0) info.NamespaceUri = reader.ReadInt32();
            if ((mask & 0x04) != 0) info.LocalizedText = reader.ReadInt32();
            if ((mask & 0x08) != 0) info.Locale = reader.ReadInt32();
            if ((mask & 0x10) != 0) info.AdditionalInfo = reader.ReadString();
            if ((mask & 0x20) != 0) info.InnerStatusCode = reader.ReadUInt32();
            if ((mask & 0x40) != 0) info.InnerDiagnosticInfo = Decode(reader); // Recursion

            return info;
        }

        /// <summary>
        /// Encodes the DiagnosticInfo object using the provided OpcUaBinaryWriter.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void Encode(OpcUaBinaryWriter writer)
        {
            ///TODO: Implement encoding? Is it needed?

            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"SymbolicId={SymbolicId}, NamespaceUri={NamespaceUri}, LocalizedText={LocalizedText}, Locale={Locale}, AdditionalInfo={AdditionalInfo}, InnerStatusCode={InnerStatusCode}, InnerDiagnosticInfo={InnerDiagnosticInfo}";
        }
    }
}