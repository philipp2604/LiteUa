using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Method
{
    /// <summary>
    /// Represents a CallResponse message in OPC UA.
    /// </summary>
    public class CallResponse
    {
        /// <summary>
        /// Gets the NodeId for the CallResponse type.
        /// </summary>
        public static readonly NodeId NodeId = new(715);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> of the CallResponse.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the array of <see cref="CallMethodResponse"/> results from the CallResponse.
        /// </summary>
        public CallMethodResponse?[]? Results { get; set; }

        /// <summary>
        /// Gets or sets the array of <see cref="DiagnosticInfo"/> from the CallResponse.
        /// </summary>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes a CallResponse using the given <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);

            int count = reader.ReadInt32();
            if (count > 0)
            {
                Results = new CallMethodResponse[count];
                for (int i = 0; i < count; i++) Results[i] = CallMethodResponse.Decode(reader);
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