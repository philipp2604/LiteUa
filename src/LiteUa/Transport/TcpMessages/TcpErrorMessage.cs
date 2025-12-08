using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Transport.TcpMessages
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    internal class TcpErrorMessage
    {
        public uint ErrorCode { get; private set; }
        public string? Reason { get; private set; }

        public void Decode(OpcUaBinaryReader reader)
        {
            ErrorCode = reader.ReadUInt32();
            Reason = reader.ReadString();
        }
    }
}
