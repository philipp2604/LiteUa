using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.SecureChannel
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class CloseSecureChannelResponse
    {
        public static readonly NodeId NodeId = new(455);

        public ResponseHeader? ResponseHeader { get; set; }

        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);
        }
    }
}