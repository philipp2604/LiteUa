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

    public class SubscriptionAcknowledgement
    {
        public uint SubscriptionId { get; set; }
        public uint SequenceNumber { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteUInt32(SubscriptionId);
            writer.WriteUInt32(SequenceNumber);
        }
    }
}
