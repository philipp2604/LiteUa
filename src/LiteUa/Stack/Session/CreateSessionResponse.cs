using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Discovery;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Session
{
    /// <summary>
    /// Represents a CreateSessionResponse message used to respond to a CreateSessionRequest in OPC UA.
    /// </summary>
    public class CreateSessionResponse
    {
        /// <summary>
        /// Gets the NodeId for the CreateSessionResponse message.
        /// </summary>
        public static readonly NodeId NodeId = new(464);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> for the CreateSessionResponse message.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the SessionId assigned by the server.
        /// </summary>
        public NodeId? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the AuthenticationToken assigned by the server.
        /// </summary>
        public NodeId? AuthenticationToken { get; set; }

        /// <summary>
        /// Gets or sets the revised session timeout in milliseconds.
        /// </summary>
        public double RevisedSessionTimeout { get; set; }

        /// <summary>
        /// Gets or sets the server nonce.
        /// </summary>
        public byte[]? ServerNonce { get; set; }

        /// <summary>
        /// Gets or sets the server certificate.
        /// </summary>
        public byte[]? ServerCertificate { get; set; }

        /// <summary>
        /// Gets or sets the array of server endpoints.
        /// </summary>
        public EndpointDescription[]? ServerEndpoints { get; set; }

        /// <summary>
        /// Gets or sets the array of server software certificates.
        /// </summary>
        public byte[]? ServerSoftwareCertificates { get; set; }

        /// <summary>
        /// Gets or sets the server signature.
        /// </summary>
        public SignatureData? ServerSignature { get; set; }

        /// <summary>
        /// Gets or sets the maximum request message size in bytes.
        /// </summary>
        public uint MaxRequestMessageSize { get; set; }

        /// <summary>
        /// Decodes a CreateSessionResponse message from the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);
            SessionId = NodeId.Decode(reader);
            AuthenticationToken = NodeId.Decode(reader);
            RevisedSessionTimeout = reader.ReadDouble();
            ServerNonce = reader.ReadByteString();
            ServerCertificate = reader.ReadByteString();

            // Endpoints Array
            int count = reader.ReadInt32();
            if (count > 0)
            {
                ServerEndpoints = new EndpointDescription[count];
                for (int i = 0; i < count; i++) ServerEndpoints[i] = EndpointDescription.Decode(reader);
            }

            /// TODO: Implement software certificates handling
            int swCount = reader.ReadInt32();
            if (swCount > 0) throw new System.NotImplementedException("Software Certificates decoding not implemented.");

            // ServerSignature (Algorithm String + Signature ByteString)
            ServerSignature = new SignatureData
            {
                Algorithm = reader.ReadString(),
                Signature = reader.ReadByteString()
            };

            MaxRequestMessageSize = reader.ReadUInt32();
        }
    }
}