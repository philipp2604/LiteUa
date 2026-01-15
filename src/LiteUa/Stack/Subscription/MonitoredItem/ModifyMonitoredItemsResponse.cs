using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// <summary>
    /// Represents a ModifyMonitoredItemsResponse message in the OPC UA protocol.
    /// </summary>
    public class ModifyMonitoredItemsResponse
    {
        /// <summary>
        /// Gets the NodeId for the ModifyMonitoredItemsResponse message type.
        /// </summary>
        public static readonly NodeId NodeId = new(766);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> of the ModifyMonitoredItemsResponse.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the results of the ModifyMonitoredItemsRequest.
        /// </summary>
        public MonitoredItemModifyResult[]? Results { get; set; }

        /// <summary>
        /// Gets or sets the diagnostic information for the ModifyMonitoredItemsRequest.
        /// </summary>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes a ModifyMonitoredItemsResponse using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
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