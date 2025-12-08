using LiteUa.BuiltIn;
using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Transport.Headers
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class RequestHeader
    {
        public NodeId AuthenticationToken { get; set; } = new NodeId(0); // ns=0;i=0 (no session yet/anonymous)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public uint RequestHandle { get; set; } = 0;
        public uint ReturnDiagnostics { get; set; } = 0;
        public string? AuditEntryId { get; set; } = null;
        public uint TimeoutHint { get; set; } = 10000; // milliseconds
        public ExtensionObject AdditionalHeader { get; set; } = new ExtensionObject();

        public void Encode(OpcUaBinaryWriter writer)
        {
            AuthenticationToken.Encode(writer);
            writer.WriteDateTime(Timestamp);
            writer.WriteUInt32(RequestHandle);
            writer.WriteUInt32(ReturnDiagnostics);
            writer.WriteString(AuditEntryId);
            writer.WriteUInt32(TimeoutHint);

            if (AdditionalHeader == null)
            {
                ExtensionObject.Null.Encode(writer);
            }
            else
            {
                AdditionalHeader.Encode(writer);
            }
        }
    }
}
