using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represents the response returned from a Browse operation in OPC UA, containing the results of browsing nodes and any associated diagnostic information.
    /// </summary>
    public class BrowseResponse
    {
        /// <summary>
        /// Gets the NodeId for the BrowseResponse type.
        /// </summary>
        public static readonly NodeId NodeId = new(530);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> associated with the operation.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the collection of browse results returned by the operation.
        /// </summary>
        public BrowseResult[]? Results { get; set; }

        /// <summary>
        /// Gets or sets the collection of diagnostic information associated with the operation.
        /// </summary>
        /// <remarks>Each element in the array corresponds to a diagnostic result for a specific item in
        /// the operation, or is null if no diagnostic information is available for that item. The length and order of
        /// the array typically match the items processed by the operation.</remarks>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes a <see cref="BrowseResponse"/> using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);

            int count = reader.ReadInt32();
            if (count > 0)
            {
                Results = new BrowseResult[count];
                for (int i = 0; i < count; i++) Results[i] = BrowseResult.Decode(reader);
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