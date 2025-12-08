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

    public class MonitoredItemNotification
    {
        public uint ClientHandle { get; set; }
        public DataValue? Value { get; set; }

        public static MonitoredItemNotification Decode(OpcUaBinaryReader reader)
        {
            return new MonitoredItemNotification
            {
                ClientHandle = reader.ReadUInt32(),
                Value = DataValue.Decode(reader)
            };
        }
    }
}
