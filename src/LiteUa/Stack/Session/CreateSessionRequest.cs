using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Session
{
    /// <summary>
    /// Represents a CreateSessionRequest message used to create a session in OPC UA.
    /// </summary>
    public class CreateSessionRequest
    {
        /// <summary>
        /// Gets the NodeId for the CreateSessionRequest message.
        /// </summary>
        public static readonly NodeId NodeId = new(461);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> for the CreateSessionRequest message.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the <see cref="ClientDescription"/> for the client creating the session.
        /// </summary>
        public ClientDescription? ClientDescription { get; set; }

        /// <summary>
        /// Gets or sets the server URI.
        /// </summary>
        public string? ServerUri { get; set; }

        /// <summary>
        ///  Gets or sets the endpoint URL.
        /// </summary>
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the session name.
        /// </summary>
        public string? SessionName { get; set; }

        /// <summary>
        /// Gets or sets the client nonce.
        /// </summary>
        public byte[]? ClientNonce { get; set; }

        /// <summary>
        /// Gets or sets the client certificate.
        /// </summary>
        public byte[]? ClientCertificate { get; set; }

        /// <summary>
        /// Gets or sets the requested session timeout in milliseconds. Default is 60000 ms (1 minute).
        /// </summary>
        public double RequestedSessionTimeout { get; set; } = 60000; // 1 min

        /// <summary>
        /// Gets or sets the maximum response message size in bytes. Default is 0, which means no limit.
        /// </summary>
        public uint MaxResponseMessageSize { get; set; } = 0; // 0 = no limit

        /// <summary>
        /// Encodes the CreateSessionRequest message using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
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