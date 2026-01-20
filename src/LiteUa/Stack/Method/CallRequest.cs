using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Method
{
    /// <summary>
    /// Represents a CallRequest message used to invoke methods on an OPC UA server.
    /// </summary>
    /// <param name="methodsToCall"></param>
    public class CallRequest(CallMethodRequest[] methodsToCall)
    {
        /// <summary>
        /// Gets the NodeId of the CallRequest type.
        /// </summary>
        public static readonly NodeId NodeId = new(712);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> containing metadata about the request.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets the array of <see cref="CallMethodRequest"/> representing the methods to be called.
        /// </summary>
        public CallMethodRequest[] MethodsToCall { get; set; } = methodsToCall;

        /// <summary>
        /// Encodes the CallRequest using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);

            if (MethodsToCall == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(MethodsToCall.Length);
                foreach (var m in MethodsToCall) m.Encode(writer);
            }
        }
    }
}