using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Transport.Headers
{
    /// <summary>
    /// Represents the header information included in responses from an OPC UA server.
    /// </summary>

    public class ResponseHeader
    {
        /// <summary>
        /// Gets or sets the timestamp indicating when the response was generated.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the request handle that matches the corresponding request.
        /// </summary>
        public uint RequestHandle { get; set; }

        /// <summary>
        /// Gets or sets the result code of the service operation.
        /// </summary>
        public uint ServiceResult { get; set; } // StatusCode

        /// <summary>
        /// Gets or sets the diagnostic information related to the service operation.
        /// </summary>
        public DiagnosticInfo? ServiceDiagnostics { get; set; }

        /// <summary>
        /// Gets or sets the string table containing additional string information.
        /// </summary>
        public string?[]? StringTable { get; set; }

        /// <summary>
        /// Gets or sets any additional header information as an extension object.
        /// </summary>
        public ExtensionObject? AdditionalHeader { get; set; }

        /// <summary>
        /// Decodes a <see cref="ResponseHeader"/> using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded <see cref="ResponseHeader"/> instance.</returns>
        public static ResponseHeader Decode(OpcUaBinaryReader reader)
        {
            var header = new ResponseHeader
            {
                Timestamp = reader.ReadDateTime(),
                RequestHandle = reader.ReadUInt32(),
                ServiceResult = reader.ReadUInt32(),

                ServiceDiagnostics = DiagnosticInfo.Decode(reader)
            };

            int count = reader.ReadInt32();
            if (count > 0)
            {
                header.StringTable = new string[count];
                for (int i = 0; i < count; i++)
                {
                    header.StringTable[i] = reader.ReadString();
                }
            }
            else
            {
                header.StringTable = [];
            }

            header.AdditionalHeader = ExtensionObject.Decode(reader);

            return header;
        }
    }
}