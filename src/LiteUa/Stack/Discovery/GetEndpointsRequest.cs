using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Discovery
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class GetEndpointsRequest
    {
        public static readonly NodeId NodeId = new(428);

        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        public string? EndpointUrl { get; set; }
        public string[]? LocaleIds { get; set; }
        public string[]? ProfileUris { get; set; }

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