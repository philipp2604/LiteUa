using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Transport.Headers
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class SequenceHeader
    {
        public uint SequenceNumber { get; set; }
        public uint RequestId { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteUInt32(SequenceNumber);
            writer.WriteUInt32(RequestId);
        }
    }
}
