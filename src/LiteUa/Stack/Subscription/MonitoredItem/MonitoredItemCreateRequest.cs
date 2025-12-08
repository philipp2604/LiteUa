using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
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

    public class MonitoredItemCreateRequest(ReadValueId itemToMonitor, uint monitoringMode, MonitoringParameters requestedParameters)
    {
        public ReadValueId ItemToMonitor { get; set; } = itemToMonitor;
        public uint MonitoringMode { get; set; } = monitoringMode;
        public MonitoringParameters RequestedParameters { get; set; } = requestedParameters;

        public void Encode(OpcUaBinaryWriter writer)
        {
            ItemToMonitor.Encode(writer);
            writer.WriteUInt32(MonitoringMode);
            RequestedParameters.Encode(writer);
        }
    }
}
