using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.BuiltIn
{
    /// TODO: Add unit tests

    /// <summary>
    /// Represents an ExpandedNodeId in OPC UA, which includes a NodeId, an optional NamespaceUri, and a ServerIndex.
    /// </summary>
    public class ExpandedNodeId
    {
        /// <summary>
        /// Gets or sets the NodeId component of the ExpandedNodeId.
        /// </summary>
        public NodeId NodeId { get; set; } = new NodeId(0);

        /// <summary>
        /// Gets or sets the optional NamespaceUri component of the ExpandedNodeId.
        /// </summary>
        public string? NamespaceUri { get; set; }

        /// <summary>
        /// Gets or sets the ServerIndex component of the ExpandedNodeId.
        /// </summary>
        public uint ServerIndex { get; set; }

        /// <summary>
        /// Decodes an ExpandedNodeId using the provided OpcUaBinaryReader.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded instance of ExpandedNodeId.</returns>
        public static ExpandedNodeId Decode(OpcUaBinaryReader reader)
        {
            byte encodingByte = reader.ReadByte();

            bool hasNamespaceUri = (encodingByte & 0x80) != 0;
            bool hasServerIndex = (encodingByte & 0x40) != 0;

            var eni = new ExpandedNodeId();
            int type = encodingByte & 0x0F;

            switch (type)
            {
                case 0: eni.NodeId = new NodeId(0, (uint)reader.ReadByte()); break; // TwoByte
                case 1: eni.NodeId = new NodeId(reader.ReadByte(), reader.ReadUInt16()); break; // FourByte
                case 2: eni.NodeId = new NodeId(reader.ReadUInt16(), reader.ReadUInt32()); break; // Numeric
                case 3: eni.NodeId = new NodeId(reader.ReadUInt16(), reader.ReadString() ?? string.Empty); break; // String
                case 4: eni.NodeId = new NodeId(reader.ReadUInt16(), reader.ReadGuid()); break; // Guid
                case 5: eni.NodeId = new NodeId(reader.ReadUInt16(), reader.ReadByteString() ?? []); break; // ByteString
            }

            if (hasNamespaceUri) eni.NamespaceUri = reader.ReadString();
            if (hasServerIndex) eni.ServerIndex = reader.ReadUInt32();

            return eni;
        }

        /// <summary>
        /// Encodes the ExpandedNodeId using the provided OpcUaBinaryWriter.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void Encode(OpcUaBinaryWriter writer)
        {
            ///TODO: Implement encoding.

            throw new NotImplementedException();
        }

        public override string ToString() => NodeId?.ToString() ?? string.Empty;
    }
}
