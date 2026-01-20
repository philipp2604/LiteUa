using LiteUa.Encoding;

namespace LiteUa.BuiltIn
{
    /// <summary>
    /// Represents a LocalizedText in OPC UA, which includes an optional Locale and Text.
    /// </summary>
    public class LocalizedText
    {
        /// <summary>
        /// Gets or sets the Locale of the LocalizedText.
        /// </summary>
        public string? Locale { get; set; }

        /// <summary>
        /// Gets or sets the Text of the LocalizedText.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Decodes a LocalizedText using the given OpcUaBinaryReader.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded instance of <see cref="LocalizedText"/>.</returns>
        public static LocalizedText Decode(OpcUaBinaryReader reader)
        {
            byte mask = reader.ReadByte();
            var lt = new LocalizedText();

            if ((mask & 0x01) != 0) lt.Locale = reader.ReadString();
            if ((mask & 0x02) != 0) lt.Text = reader.ReadString();

            return lt;
        }

        /// <summary>
        /// Encodes the current object's locale and text values into the specified binary writer using the OPC UA binary
        /// encoding format.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            byte mask = 0;
            if (Locale != null) mask |= 0x01;
            if (Text != null) mask |= 0x02;

            writer.WriteByte(mask);
            if (Locale != null) writer.WriteString(Locale);
            if (Text != null) writer.WriteString(Text);
        }

        public override string ToString() => Text ?? string.Empty;

        public static implicit operator string(LocalizedText lt) => lt?.Text ?? string.Empty;

        public static implicit operator LocalizedText(string text) => new() { Text = text };
    }
}