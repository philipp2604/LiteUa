using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// Represents the result of translating a browse path in an OPC UA address space, including the status of the
    /// operation and any target nodes found.
    /// </summary>
    /// <remarks>A browse path translation resolves a sequence of relative path elements to one or more target
    /// nodes in the OPC UA server's address space. This class encapsulates both the outcome of the translation and the
    /// set of targets, if any, that were found. The contents of the Targets property depend on the StatusCode; if the
    /// translation fails, Targets may be null or empty.</remarks>
    public class BrowsePathResult
    {
        /// <summary>
        /// Gets or sets the status code associated with the response.
        /// </summary>
        public StatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the collection of browse path targets associated with this instance.
        /// </summary>
        public BrowsePathTarget[]? Targets { get; set; }

        /// <summary>
        /// Decodes a BrowsePathResult using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>A new instance of <see cref="BrowsePathResult"/>.</returns>
        public static BrowsePathResult Decode(OpcUaBinaryReader reader)
        {
            var res = new BrowsePathResult
            {
                StatusCode = StatusCode.Decode(reader)
            };

            int count = reader.ReadInt32();
            if (count > 0)
            {
                res.Targets = new BrowsePathTarget[count];
                for (int i = 0; i < count; i++) res.Targets[i] = BrowsePathTarget.Decode(reader);
            }
            return res;
        }
    }
}