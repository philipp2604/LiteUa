using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represemts the result of a browse operation in an OPC UA address space, including the status, continuation point, and references found.
    /// </summary>
    public class BrowseResult
    {
        /// <summary>
        /// Gets or sets the status code associated with the response.
        /// </summary>
        public StatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the continuation points for the browse operation.
        /// </summary>
        public byte[]? ContinuationPoint { get; set; }

        /// <summary>
        /// Gets or sets the collection of reference descriptions associated with this browse result.
        /// </summary>
        public ReferenceDescription[]? References { get; set; }

        /// <summary>
        /// Decodes a <see cref="BrowseResult"/> using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>A new instance of <see cref="BrowseResult"/> based on the decoded data.</returns>
        public static BrowseResult Decode(OpcUaBinaryReader reader)
        {
            var res = new BrowseResult
            {
                StatusCode = StatusCode.Decode(reader),
                ContinuationPoint = reader.ReadByteString()
            };

            int count = reader.ReadInt32();
            if (count > 0)
            {
                res.References = new ReferenceDescription[count];
                for (int i = 0; i < count; i++) res.References[i] = ReferenceDescription.Decode(reader);
            }
            return res;
        }
    }
}