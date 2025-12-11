using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Subscription.MonitoredItem
{
    public class MonitoredItemModifyRequest(uint monitoredItemId, MonitoringParameters requestedParameters)
    {
        public uint MonitoredItemId { get; set; } = monitoredItemId;
        public MonitoringParameters RequestedParameters { get; set; } = requestedParameters;

        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteUInt32(MonitoredItemId);
            RequestedParameters.Encode(writer);
        }
    }
}
