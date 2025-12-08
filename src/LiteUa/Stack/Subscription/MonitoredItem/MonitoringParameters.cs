using LiteUa.BuiltIn;
using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class MonitoringParameters
    {
        public uint ClientHandle { get; set; }
        public double SamplingInterval { get; set; } = -1; // Default
        public ExtensionObject? Filter { get; set; } // Null for DataChange
        public uint QueueSize { get; set; } = 0;
        public bool DiscardOldest { get; set; } = true;

        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteUInt32(ClientHandle);
            writer.WriteDouble(SamplingInterval);
            // Filter
            if (Filter == null) new ExtensionObject().Encode(writer); // Null/Empty
            else Filter.Encode(writer);

            writer.WriteUInt32(QueueSize);
            writer.WriteBoolean(DiscardOldest);
        }
    }
}
