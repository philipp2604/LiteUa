using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a DeleteSubscriptionsResponse message used to respond to a DeleteSubscriptionsRequest in OPC UA.
    /// </summary>
    public class DeleteSubscriptionsResponse
    {
        /// <summary>
        /// Gets the NodeId for the DeleteSubscriptionsResponse message.
        /// </summary>
        public static readonly NodeId NodeId = new(850);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> for the DeleteSubscriptionsResponse message.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the array of StatusCodes indicating the result of each subscription deletion.
        /// </summary>
        public StatusCode[]? Results { get; set; }

        /// <summary>
        /// Gets or sets the array of <see cref="DiagnosticInfo"/> for the DeleteSubscriptionsResponse.
        /// </summary>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes a DeleteSubscriptionsResponse message using the provided <see cref="OpcUaBinaryReader"/>.
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