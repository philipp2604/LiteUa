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
    
    public class StatusChangeNotification
    {
        public StatusCode Status { get; set; }
        public DiagnosticInfo? DiagnosticInfo { get; set; }

        public static StatusChangeNotification Decode(OpcUaBinaryReader reader)
        {
            var scn = new StatusChangeNotification
            {
                Status = StatusCode.Decode(reader),
                DiagnosticInfo = DiagnosticInfo.Decode(reader)
            };
            return scn;
        }
    }
}
