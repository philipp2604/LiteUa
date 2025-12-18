using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method
    public class ModifyMonitoredItemsResponse
    {
        public static readonly NodeId NodeId = new(766);
        public ResponseHeader? ResponseHeader { get; set; }
        public MonitoredItemModifyResult[]? Results { get; set; }
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);

            int count = reader.ReadInt32();
            if (count > 0)
            {
                Results = new MonitoredItemModifyResult[count];
                for (int i = 0; i < count; i++) Results[i] = MonitoredItemModifyResult.Decode(reader);
            }

            if (reader.Position < reader.Length)
            {
                int diagCount = reader.ReadInt32();
                if (diagCount > 0)
                {
                    DiagnosticInfos = new DiagnosticInfo[diagCount];
                    for (int i = 0; i < diagCount; i++) DiagnosticInfos[i] = DiagnosticInfo.Decode(reader);
                }
            }
        }
    }
}