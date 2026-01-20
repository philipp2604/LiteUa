using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Attribute
{
    /// <summary>
    /// Represents a WriteResponse message in OPC UA.
    /// </summary>
    public class WriteResponse
    {
        /// <summary>
        /// Gets the NodeId for the WriteResponse type.
        /// </summary>
        public static readonly NodeId NodeId = new(676);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> of the WriteResponse.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the array of <see cref="StatusCode"/> results for the WriteResponse.
        /// </summary>
        public StatusCode[]? Results { get; set; }

        /// <summary>
        /// Gets or sets the array of <see cref="DiagnosticInfo"/> for the WriteResponse.
        /// </summary>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes a WriteResponse using the given <see cref="OpcUaBinaryReader"/>.
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