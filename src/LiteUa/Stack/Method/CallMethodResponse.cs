using LiteUa.BuiltIn;
using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Method
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class CallMethodResponse
    {
        public static readonly NodeId NodeId = new(709);

        public StatusCode StatusCode { get; set; }
        public StatusCode[]? InputArgumentResults { get; set; }
        public DiagnosticInfo?[]? InputArgumentDiagnosticInfos { get; set; }
        public Variant[]? OutputArguments { get; set; }

        public static CallMethodResponse Decode(OpcUaBinaryReader reader)
        {
            var res = new CallMethodResponse
            {
                StatusCode = StatusCode.Decode(reader)
            };

            // InputArg Results
            int count = reader.ReadInt32();
            if (count > 0)
            {
                res.InputArgumentResults = new StatusCode[count];
                for (int i = 0; i < count; i++) res.InputArgumentResults[i] = StatusCode.Decode(reader);
            }

            // InputArg Diagnostics
            int diagCount = reader.ReadInt32();
            if (diagCount > 0)
            {
                res.InputArgumentDiagnosticInfos = new DiagnosticInfo[diagCount];
                for (int i = 0; i < diagCount; i++) res.InputArgumentDiagnosticInfos[i] = DiagnosticInfo.Decode(reader);
            }

            // Output Arguments
            int outCount = reader.ReadInt32();
            if (outCount > 0)
            {
                res.OutputArguments = new Variant[outCount];
                for (int i = 0; i < outCount; i++) res.OutputArguments[i] = Variant.Decode(reader);
            }

            return res;
        }
    }
}
