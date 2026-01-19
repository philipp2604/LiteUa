using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represents the response returned from a TranslateBrowsePathsToNodeIds operation in OPC UA,
    /// </summary>
    public class TranslateBrowsePathsToNodeIdsResponse
    {
        /// <summary>
        /// Gets the NodeId for the TranslateBrowsePathsToNodeIdsResponse type.
        /// </summary>
        public static readonly NodeId NodeId = new(557);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> associated with the operation.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the collection of browse path results returned by the operation.
        /// </summary>
        public BrowsePathResult[]? Results { get; set; }

        /// <summary>
        /// Gets or sets the collection of diagnostic information associated with the operation.
        /// </summary>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes the <see cref="TranslateBrowsePathsToNodeIdsResponse"/> using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding the instance.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);

            int count = reader.ReadInt32();
            if (count > 0)
            {
                Results = new BrowsePathResult[count];
                for (int i = 0; i < count; i++) Results[i] = BrowsePathResult.Decode(reader);
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