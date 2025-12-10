using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.View
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method
    public class BrowseNextResponse
    {
        public static readonly NodeId NodeId = new(536);

        public ResponseHeader? ResponseHeader { get; set; }
        public BrowseResult[]? Results { get; set; }
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        public void Decode(OpcUaBinaryReader reader)
        {
            // Exakt wie BrowseResponse.Decode
            ResponseHeader = ResponseHeader.Decode(reader);

            int count = reader.ReadInt32();
            if (count > 0)
            {
                Results = new BrowseResult[count];
                for (int i = 0; i < count; i++) Results[i] = BrowseResult.Decode(reader);
            }

            int diagCount = reader.ReadInt32();
            if (diagCount > 0)
            {
                DiagnosticInfos = new DiagnosticInfo[count];
                for (int i = 0; i < count; i++)
                {
                    DiagnosticInfos[i] = DiagnosticInfo.Decode(reader);
                }
            }
        }
    }
}
