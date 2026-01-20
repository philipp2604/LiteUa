using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Method
{
    /// <summary>
    /// Represents a CallMethodResponse message in OPC UA.
    /// </summary>
    public class CallMethodResponse
    {
        /// <summary>
        /// Gets the NodeId for CallMethodResponse.
        /// </summary>
        public static readonly NodeId NodeId = new(709);

        /// <summary>
        /// Gets or sets the status code of the method call.
        /// </summary>
        public StatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the array of status codes for each input argument.
        /// </summary>
        public StatusCode[]? InputArgumentResults { get; set; }

        /// <summary>
        /// Gets or sets the array of diagnostic information for each input argument.
        /// </summary>
        public DiagnosticInfo?[]? InputArgumentDiagnosticInfos { get; set; }

        /// <summary>
        /// Gets or sets the array of output arguments returned by the method call.
        /// </summary>
        public Variant[]? OutputArguments { get; set; }

        /// <summary>
        /// Decodes a CallMethodResponse using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for encoding.</param>
        /// <returns>The decoded <see cref="CallMethodResponse"/> instance.</returns>
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