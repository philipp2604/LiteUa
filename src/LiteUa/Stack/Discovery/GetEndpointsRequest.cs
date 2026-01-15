using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Discovery
{
    /// <summary>
    /// Represents a GetEndpointsRequest message used to request the endpoints of an OPC UA server.
    /// </summary>
    public class GetEndpointsRequest
    {
        /// <summary>
        ///  Gets the NodeId for the GetEndpointsRequest type.
        /// </summary>
        public static readonly NodeId NodeId = new(428);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> containing metadata about the request.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the URL of the endpoint to retrieve.
        /// </summary>
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the array of locale IDs to filter the endpoints.
        /// </summary>
        public string[]? LocaleIds { get; set; }

        /// <summary>
        /// Gets or sets the array of profile URIs to filter the endpoints.
        /// </summary>
        public string[]? ProfileUris { get; set; }

        /// <summary>
        /// Encodes the GetEndpointsRequest using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            writer.WriteString(EndpointUrl);

            if (LocaleIds == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(LocaleIds.Length);
                foreach (var s in LocaleIds) writer.WriteString(s);
            }

            if (ProfileUris == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(ProfileUris.Length);
                foreach (var s in ProfileUris) writer.WriteString(s);
            }
        }
    }
}