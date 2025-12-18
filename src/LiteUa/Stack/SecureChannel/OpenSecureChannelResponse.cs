using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Security;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.SecureChannel
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class OpenSecureChannelResponse
    {
        public static readonly NodeId NodeId = new(449);

        public ResponseHeader? ResponseHeader { get; set; }
        public uint ServerProtocolVersion { get; set; }
        public ChannelSecurityToken? SecurityToken { get; set; }
        public byte[]? ServerNonce { get; set; }

        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);
            ServerProtocolVersion = reader.ReadUInt32();
            SecurityToken = ChannelSecurityToken.Decode(reader);
            ServerNonce = reader.ReadByteString();
        }
    }
}