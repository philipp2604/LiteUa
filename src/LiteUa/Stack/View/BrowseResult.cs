using LiteUa.BuiltIn;
using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.View
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class BrowseResult
    {
        public StatusCode StatusCode { get; set; }
        public byte[]? ContinuationPoint { get; set; }
        public ReferenceDescription[]? References { get; set; }

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
