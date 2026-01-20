using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Method
{
    /// <summary>
    /// Represents a CallMethodRequest used to invoke a method on an OPC UA server.
    /// </summary>
    /// <param name="objectId">The method's object id.</param>
    /// <param name="methodId">The method's id.</param>
    /// <param name="inputArguments">Input arguments for the method call.</param>
    public class CallMethodRequest(NodeId objectId, NodeId methodId, Variant[]? inputArguments = null)
    {
        /// <summary>
        /// Gets the NodeId for the CallMethodRequest message type.
        /// </summary>
        public static readonly NodeId NodeId = new(706);

        /// <summary>
        /// Gets or sets the ObjectId on which the method is called.
        /// </summary>
        public NodeId ObjectId { get; set; } = objectId;

        /// <summary>
        /// Gets or sets the MethodId of the method to be called.
        /// </summary>
        public NodeId MethodId { get; set; } = methodId;

        /// <summary>
        /// Gets or sets the array of input arguments for the method call.
        /// </summary>
        public Variant[]? InputArguments { get; set; } = inputArguments;

        /// <summary>
        /// Encodes the CallMethodRequest using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            ObjectId.Encode(writer);
            MethodId.Encode(writer);

            if (InputArguments == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(InputArguments.Length);
                foreach (var arg in InputArguments) arg.Encode(writer);
            }
        }
    }
}