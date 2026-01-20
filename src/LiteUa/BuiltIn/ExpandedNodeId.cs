using LiteUa.Encoding;

namespace LiteUa.BuiltIn
{
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
        /// <summary>
        public void Encode(OpcUaBinaryWriter writer)
        {
            if (NodeId == null)
            {
                throw new InvalidOperationException("NodeId cannot be null.");
            }

            byte encodingByte;

            // set encoding byte
            if (NodeId.StringIdentifier != null)
            {
                encodingByte = 0x03; // String
            }
            else if (NodeId.GuidIdentifier.HasValue)
            {
                encodingByte = 0x04; // Guid
            }
            else if (NodeId.ByteStringIdentifier != null)
            {
                encodingByte = 0x05; // ByteString
            }
            else if (NodeId.NumericIdentifier.HasValue)
            {
                if (NodeId.NamespaceIndex == 0 && NodeId.NumericIdentifier <= 255)
                {
                    encodingByte = 0x00; // TwoByte
                }
                else if (NodeId.NamespaceIndex <= 255 && NodeId.NumericIdentifier <= 65535)
                {
                    encodingByte = 0x01; // FourByte
                }
                else
                {
                    encodingByte = 0x02; // Numeric
                }
            }
            else
            {
                throw new NotSupportedException("The NodeId contains no valid identifier.");
            }

            // 2. add flags
            bool hasNamespaceUri = !string.IsNullOrEmpty(NamespaceUri);
            bool hasServerIndex = ServerIndex != 0;

            if (hasNamespaceUri) encodingByte |= 0x80;
            if (hasServerIndex) encodingByte |= 0x40;

            // 3. write encoding byte
            writer.WriteByte(encodingByte);

            // 4. write node id
            int type = encodingByte & 0x0F;
            switch (type)
            {
                case 0: // TwoByte
                    writer.WriteByte((byte)NodeId.NumericIdentifier!.Value);
                    break;

                case 1: // FourByte
                    writer.WriteByte((byte)NodeId.NamespaceIndex);
                    writer.WriteUInt16((ushort)NodeId.NumericIdentifier!.Value);
                    break;

                case 2: // Numeric
                    writer.WriteUInt16(NodeId.NamespaceIndex);
                    writer.WriteUInt32(NodeId.NumericIdentifier!.Value);
                    break;

                case 3: // String
                    writer.WriteUInt16(NodeId.NamespaceIndex);
                    writer.WriteString(NodeId.StringIdentifier);
                    break;

                case 4: // Guid
                    writer.WriteUInt16(NodeId.NamespaceIndex);
                    writer.WriteGuid(NodeId.GuidIdentifier!.Value);
                    break;

                case 5: // ByteString
                    writer.WriteUInt16(NodeId.NamespaceIndex);
                    writer.WriteByteString(NodeId.ByteStringIdentifier);
                    break;
            }

            // 5. write optional fields
            if (hasNamespaceUri)
            {
                writer.WriteString(NamespaceUri);
            }

            if (hasServerIndex)
            {
                writer.WriteUInt32(ServerIndex);
            }
        }

        public override string ToString() => NodeId?.ToString() ?? string.Empty;
    }
}