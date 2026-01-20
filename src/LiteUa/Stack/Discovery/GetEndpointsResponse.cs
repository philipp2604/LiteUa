using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;

namespace LiteUa.Stack.Discovery
{
    /// <summary>
    /// Represents a GetEndpointsResponse message in OPC UA.
    /// </summary>

    public class GetEndpointsResponse
    {
        /// <summary>
        /// Gets the NodeId of the GetEndpointsResponse type.
        /// </summary>
        public static readonly NodeId NodeId = new(431);

        /// <summary>
        /// Gets or sets the <see cref="ResponseHeader"/> of the GetEndpointsResponse.
        /// </summary>
        public ResponseHeader? ResponseHeader { get; set; }

        /// <summary>
        /// Gets or sets the array of <see cref="EndpointDescription"/> returned by the GetEndpointsResponse.
        /// </summary>
        public EndpointDescription[]? Endpoints { get; set; }

        /// <summary>
        /// Decodes a GetEndpointsResponse using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public void Decode(OpcUaBinaryReader reader)
        {
            ResponseHeader = ResponseHeader.Decode(reader);

            int count = reader.ReadInt32();
            if (count > 0)
            {
                Endpoints = new EndpointDescription[count];
                for (int i = 0; i < count; i++)
                {
                    Endpoints[i] = EndpointDescription.Decode(reader);
                }
            }
            else
            {
                Endpoints = [];
            }
        }
    }
}