using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.SecureChannel
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class CloseSecureChannelRequest
    {
        public static readonly NodeId NodeId = new(452);
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
        }
    }
}