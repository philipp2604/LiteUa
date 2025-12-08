using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Transport.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Discovery
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class GetEndpointsResponse
    {
        public static readonly NodeId NodeId = new(431);

        public ResponseHeader? ResponseHeader { get; set; }
        public EndpointDescription[]? Endpoints { get; set; }

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
