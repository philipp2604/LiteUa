using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Discovery;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Session
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class CreateSessionResponse
    {
        public static readonly NodeId NodeId = new(464);

        public ResponseHeader? ResponseHeader { get; set; }
        public NodeId? SessionId { get; set; }
        public NodeId? AuthenticationToken { get; set; }
        public double RevisedSessionTimeout { get; set; }
        public byte[]? ServerNonce { get; set; }
        public byte[]? ServerCertificate { get; set; }
        public EndpointDescription[]? ServerEndpoints { get; set; }
        public byte[]? ServerSoftwareCertificates { get; set; }
        public SignatureData? ServerSignature { get; set; }
        public uint MaxRequestMessageSize { get; set; }

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
            if (swCount > 0) throw new System.NotImplementedException("Software Certificates decoding not impl.");

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