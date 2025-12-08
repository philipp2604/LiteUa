using LiteUa.BuiltIn;
using LiteUa.Encoding;
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

    public class NotificationMessage
    {
        public uint SequenceNumber { get; set; }
        public DateTime PublishTime { get; set; }
        public ExtensionObject[]? NotificationData { get; set; }

        public static NotificationMessage Decode(OpcUaBinaryReader reader)
        {
            var msg = new NotificationMessage
            {
                SequenceNumber = reader.ReadUInt32(),
                PublishTime = reader.ReadDateTime()
            };

            int count = reader.ReadInt32();
            if (count > 0)
            {
                msg.NotificationData = new ExtensionObject[count];
                for (int i = 0; i < count; i++) msg.NotificationData[i] = ExtensionObject.Decode(reader);
            }
            return msg;
        }
    }
}
