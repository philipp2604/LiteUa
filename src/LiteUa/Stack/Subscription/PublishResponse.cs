using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Subscription
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class PublishResponse
    {
        public static readonly NodeId NodeId = new(829);

        public ResponseHeader? ResponseHeader { get; set; }
        public uint SubscriptionId { get; set; }
        public uint[]? AvailableSequenceNumbers { get; set; }
        public bool MoreNotifications { get; set; }
        public NotificationMessage? NotificationMessage { get; set; }
        public StatusCode[]? Results { get; set; }
        public DiagnosticInfo?[]? DiagnosticInfos { get; set; }

        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);
            SubscriptionId = reader.ReadUInt32();

            int count = reader.ReadInt32();
            if (count > 0)
            {
                AvailableSequenceNumbers = new uint[count];
                for (int i = 0; i < count; i++) AvailableSequenceNumbers[i] = reader.ReadUInt32();
            }
            else
            {
                AvailableSequenceNumbers = [];
            }

            MoreNotifications = reader.ReadBoolean();
            NotificationMessage = NotificationMessage.Decode(reader);

            // Results (Acks results)
            int resCount = reader.ReadInt32();
            if (resCount > 0)
            {
                Results = new StatusCode[resCount];
                for (int i = 0; i < resCount; i++) Results[i] = StatusCode.Decode(reader);
            }

            if (reader.Position < reader.Length)
            {
                int diagCount = reader.ReadInt32();
                if (diagCount > 0)
                {
                    DiagnosticInfos = new DiagnosticInfo[diagCount];
                    for (int i = 0; i < diagCount; i++) DiagnosticInfos[i] = DiagnosticInfo.Decode(reader);
                }
            }
        }
    }
}
