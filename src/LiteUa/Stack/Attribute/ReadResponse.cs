using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Attribute
{
    /// <summary>
    /// Represents a ReadResponse message in OPC UA.
    /// </summary>
    public class ReadResponse
    {
        /// <summary>
        /// Gets the NodeId of the ReadResponse type.
        /// </summary>
        public static readonly NodeId NodeId = new(634);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> of the ReadResponse.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the array of <see cref="DataValue"/> results from the ReadResponse.
        /// </summary>
        public DataValue[]? Results { get; set; }

        /// <summary>
        /// Gets or sets the array of <see cref="DiagnosticInfo"/> from the ReadResponse.
        /// </summary>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes a ReadResponse using the given <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);

            int count = reader.ReadInt32();
            if (count > 0)
            {
                Results = new DataValue[count];
                for (int i = 0; i < count; i++) Results[i] = DataValue.Decode(reader);
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