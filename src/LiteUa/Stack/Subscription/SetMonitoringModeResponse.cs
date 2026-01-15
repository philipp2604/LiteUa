using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a SetMonitoringModeResponse message in the OPC UA protocol.
    /// </summary>
    public class SetMonitoringModeResponse
    {
        /// <summary>
        /// Gets the NodeId for the SetMonitoringModeResponse message type.
        /// </summary>
        public static readonly NodeId NodeId = new(772);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> of the SetMonitoringModeResponse.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the results of the SetMonitoringModeResponse.
        /// </summary>
        public StatusCode[]? Results { get; set; }

        /// <summary>
        /// Gets or sets the diagnostic information for the SetMonitoringModeResponse.
        /// </summary>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes a SetMonitoringModeResponse using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);

            int count = reader.ReadInt32();
            if (count > 0)
            {
                Results = new StatusCode[count];
                for (int i = 0; i < count; i++) Results[i] = StatusCode.Decode(reader);
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