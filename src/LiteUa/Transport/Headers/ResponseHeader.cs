using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Transport.Headers
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class ResponseHeader
    {
        public DateTime Timestamp { get; set; }
        public uint RequestHandle { get; set; }
        public uint ServiceResult { get; set; } // StatusCode
        public DiagnosticInfo? ServiceDiagnostics { get; set; }
        public string?[]? StringTable { get; set; }
        public ExtensionObject? AdditionalHeader { get; set; }

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