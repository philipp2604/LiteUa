using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Session
{
    /// <summary>
    /// Represents an ActivateSessionResponse message in OPC UA.
    /// </summary>
    public class ActivateSessionResponse
    {
        /// <summary>
        /// Gets the NodeId for the ActivateSessionResponse message.
        /// </summary>
        public static readonly NodeId NodeId = new(470);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> of the ActivateSessionResponse.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        ///  Gets or sets the ServerNonce provided by the server.
        /// </summary>
        public byte[]? ServerNonce { get; set; }

        /// <summary>
        /// Gets or sets the array of StatusCodes for SoftwareCertificates validation.
        /// </summary>
        public uint[]? Results { get; set; }

        /// <summary>
        /// Gets or sets the array of <see cref="DiagnosticInfo"/> for the ActivateSessionResponse.
        /// </summary>
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        /// <summary>
        /// Decodes an ActivateSessionResponse using the given <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);
            ServerNonce = reader.ReadByteString();

            // Results Array (StatusCodes for SoftwareCerts validation)
            int count = reader.ReadInt32();
            if (count > 0)
            {
                Results = new uint[count];
                for (int i = 0; i < count; i++) Results[i] = reader.ReadUInt32();
            }

            // Diagnostics
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