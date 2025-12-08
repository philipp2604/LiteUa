using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.SecureChannel
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class OpenSecureChannelRequest
    {
        public static readonly NodeId NodeId = new(446);

        public RequestHeader RequestHeader { get; set; } = new RequestHeader();
        public uint ClientProtocolVersion { get; set; } = 0;
        public SecurityTokenRequestType RequestType { get; set; } = SecurityTokenRequestType.Issue;
        public MessageSecurityMode SecurityMode { get; set; } = MessageSecurityMode.None;
        public byte[]? ClientNonce { get; set; }
        public uint RequestedLifetime { get; set; } = 3600000; // 1h

        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteUInt32(ClientProtocolVersion);
            writer.WriteInt32((int)RequestType);
            writer.WriteInt32((int)SecurityMode);
            writer.WriteByteString(ClientNonce);
            writer.WriteUInt32(RequestedLifetime);
        }
    }
}
