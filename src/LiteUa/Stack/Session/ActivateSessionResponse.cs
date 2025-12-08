using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Session
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class ActivateSessionResponse
    {
        public static readonly NodeId NodeId = new(470);

        public ResponseHeader? ResponseHeader { get; set; }
        public byte[]? ServerNonce { get; set; }
        public uint[]? Results { get; set; }
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);
            ServerNonce = reader.ReadByteString();

            // Results Array (StatusCodes for SoftwareCerts validation)
            int count = reader.ReadInt32();
            if (count > 0)
            {
                Results = new uint[count];
                for (int i = 0; i < count; i++) Results[i] = reader.ReadUInt32();
            }

            // Diagnostics
            int diagCount = reader.ReadInt32();
            if (diagCount > 0)
            {
                DiagnosticInfos = new DiagnosticInfo[diagCount];
                for (int i = 0; i < diagCount; i++) DiagnosticInfos[i] = DiagnosticInfo.Decode(reader);
            }
        }
    }
}
