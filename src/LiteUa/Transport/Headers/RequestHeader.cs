using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Transport.Headers
{
    /// <summary>
    /// Represents the header for a request message in OPC UA.
    /// </summary>
    public class RequestHeader
    {
        /// <summary>
        /// Gets or sets the authentication token for the session.
        /// </summary>
        public NodeId AuthenticationToken { get; set; } = new NodeId(0); // ns=0;i=0 (no session yet/anonymous)

        /// <summary>
        /// Gets or sets the timestamp indicating when the request was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the unique handle for the request.
        /// </summary>
        public uint RequestHandle { get; set; } = 0;

        /// <summary>
        /// Gets or sets the diagnostic information level to be returned.
        /// </summary>
        public uint ReturnDiagnostics { get; set; } = 0;

        /// <summary>
        /// Gets or sets the audit entry identifier for the request.
        /// </summary>
        public string? AuditEntryId { get; set; } = null;

        /// <summary>
        /// Gets or sets the timeout hint for the request in milliseconds, defaulting to 10000 ms.
        /// </summary>
        public uint TimeoutHint { get; set; } = 10000; // milliseconds

        /// <summary>
        /// Gets or sets any additional header information for the request.
        /// </summary>
        public ExtensionObject AdditionalHeader { get; set; } = new ExtensionObject();

        /// <summary>
        /// Encodes the RequestHeader using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            AuthenticationToken.Encode(writer);
            writer.WriteDateTime(Timestamp);
            writer.WriteUInt32(RequestHandle);
            writer.WriteUInt32(ReturnDiagnostics);
            writer.WriteString(AuditEntryId);
            writer.WriteUInt32(TimeoutHint);

            if (AdditionalHeader == null)
            {
                ExtensionObject.Null.Encode(writer);
            }
            else
            {
                AdditionalHeader.Encode(writer);
            }
        }
    }
}