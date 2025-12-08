using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Session
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class CreateSessionRequest
    {
        public static readonly NodeId NodeId = new(461);

        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        public ClientDescription? ClientDescription { get; set; }
        public string? ServerUri { get; set; }
        public string? EndpointUrl { get; set; }
        public string? SessionName { get; set; }
        public byte[]? ClientNonce { get; set; }
        public byte[]? ClientCertificate { get; set; }
        public double RequestedSessionTimeout { get; set; } = 60000; // 1 min
        public uint MaxResponseMessageSize { get; set; } = 0; // 0 = no limit

        public void Encode(OpcUaBinaryWriter writer)
        {
            ArgumentNullException.ThrowIfNull(ClientDescription, nameof(ClientDescription));

            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            ClientDescription.Encode(writer);
            writer.WriteString(ServerUri);
            writer.WriteString(EndpointUrl);
            writer.WriteString(SessionName);
            writer.WriteByteString(ClientNonce);
            writer.WriteByteString(ClientCertificate);
            writer.WriteDouble(RequestedSessionTimeout);
            writer.WriteUInt32(MaxResponseMessageSize);
        }
    }
}
