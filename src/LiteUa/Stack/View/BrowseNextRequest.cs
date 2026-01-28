using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represents a request to retrieve the next set of browse results in an OPC UA session using continuation points.
    /// </summary>
    public class BrowseNextRequest
    {
        /// <summary>
        /// Gets the unique identifier for the BrowseNextRequest type.
        /// </summary>
        public static readonly NodeId NodeId = new(533);

        /// <summary>
        /// Gets or sets the <see cref="RequestHeader"/> information for the operation.
        /// </summary>
        public RequestHeader RequestHeader { get; set; } = new RequestHeader();

        /// <summary>
        /// Gets or sets a value indicating whether continuation points should be released after use.
        /// </summary>
        /// <remarks>Set this property to <see langword="true"/> to automatically release continuation
        /// points when they are no longer needed, which can help conserve server resources. If set to <see
        /// langword="false"/>, continuation points must be managed and released manually.</remarks>
        public bool ReleaseContinuationPoints { get; set; } = false;

        /// <summary>
        /// Gets or sets the collection of continuation points used to resume a previously interrupted operation.
        /// </summary>
        /// <remarks>Each continuation point is represented as a byte array. This property is typically
        /// used in scenarios where large data sets are retrieved in multiple parts, allowing the operation to continue
        /// from where it left off.</remarks>
        public byte[][]? ContinuationPoints { get; set; }

        /// <summary>
        /// Encodes the current object using the specified <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> used to decode the object's data. Cannot be null.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            NodeId.Encode(writer);
            RequestHeader.Encode(writer);
            writer.WriteBoolean(ReleaseContinuationPoints);

            if (ContinuationPoints == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(ContinuationPoints.Length);
                foreach (var cp in ContinuationPoints) writer.WriteByteString(cp);
            }
        }
    }
}