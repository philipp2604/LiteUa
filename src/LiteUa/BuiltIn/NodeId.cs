using LiteUa.Encoding;
using System.Text;

namespace LiteUa.BuiltIn
{
    /// <summary>
    /// A class representing a NodeId in OPC UA.
    /// </summary>
    public class NodeId
    {
        private enum NodeIdEncoding : byte
        {
            TwoByte = 0x00,
            FourByte = 0x01,
            Numeric = 0x02,
            String = 0x03,
            Guid = 0x04,
            ByteString = 0x05,

            TypeMask = 0x0F,
            ServerIndexFlag = 0x40,
            NamespaceUriFlag = 0x80
        }

        /// <summary>
        /// Initializes a new instance of the NodeId structure with the specified numeric identifier and a default
        /// namespace index of 0.
        /// </summary>
        /// <param name="id">The numeric identifier for the node. This value uniquely identifies the node within the default namespace.</param>
        public NodeId(uint id)
        { NamespaceIndex = 0; NumericIdentifier = id; }

        /// <summary>
        /// Initializes a new instance of the NodeId structure with the specified namespace index and numeric
        /// identifier.
        /// </summary>
        /// <param name="ns">The namespace index that identifies the namespace of the node. Must be a valid namespace index as defined by
        /// the application context.</param>
        /// <param name="id">The numeric identifier for the node within the specified namespace.</param>
        public NodeId(ushort ns, uint id)
        { NamespaceIndex = ns; NumericIdentifier = id; }

        /// <summary>
        /// Initializes a new instance of the NodeId class using the specified namespace index and string identifier.
        /// </summary>
        /// <param name="ns">The namespace index that identifies the namespace of the node. Must be a non-negative value.</param>
        /// <param name="id">The string identifier that uniquely identifies the node within the specified namespace. Cannot be null.</param>
        public NodeId(ushort ns, string id)
        { NamespaceIndex = ns; StringIdentifier = id; }

        /// <summary>
        /// Initializes a new instance of the NodeId structure using the specified namespace index and GUID identifier.
        /// </summary>
        /// <param name="ns">The namespace index that identifies the namespace of the node. Must be a valid namespace index as defined by
        /// the application context.</param>
        /// <param name="id">The GUID that uniquely identifies the node within the specified namespace.</param>
        public NodeId(ushort ns, Guid id)
        { NamespaceIndex = ns; GuidIdentifier = id; }

        /// <summary>
        /// Initializes a new instance of the NodeId class using the specified namespace index and byte string
        /// identifier.
        /// </summary>
        /// <param name="ns">The namespace index that identifies the namespace of the node. Must be a valid namespace index as defined by
        /// the application context.</param>
        /// <param name="id">A byte array that uniquely identifies the node within the specified namespace. Cannot be null.</param>
        public NodeId(ushort ns, byte[] id)
        { NamespaceIndex = ns; ByteStringIdentifier = id; }

        /// <summary>
        /// Gets the NamespaceIndex of the NodeId.
        /// </summary>
        public ushort NamespaceIndex { get; private set; }

        /// <summary>
        /// Gets the NumericIdentifier of the NodeId, if applicable.
        /// </summary>
        public uint? NumericIdentifier { get; private set; }

        /// <summary>
        /// Gets the StringIdentifier of the NodeId, if applicable.
        /// </summary>
        public string? StringIdentifier { get; private set; }

        /// <summary>
        /// Gets the GuidIdentifier of the NodeId, if applicable.
        /// </summary>
        public Guid? GuidIdentifier { get; private set; }

        /// <summary>
        /// Gets the ByteStringIdentifier of the NodeId, if applicable.
        /// </summary>
        public byte[]? ByteStringIdentifier { get; private set; }

        /// <summary>
        /// Gets the NamespaceUri of the NodeId, if applicable.
        /// </summary>
        public string? NamespaceUri { get; private set; }

        /// <summary>
        /// Gets the ServerIndex of the NodeId, if applicable.
        /// </summary>
        public uint ServerIndex { get; private set; }

        /// <summary>
        /// Encodes the NodeId using the provided OpcUaBinaryWriter.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void Encode(OpcUaBinaryWriter writer)
        {
            if (NamespaceIndex == 0 && NumericIdentifier.HasValue && NumericIdentifier.Value <= 255)
            {
                writer.WriteByte((byte)NodeIdEncoding.TwoByte);
                writer.WriteByte((byte)NumericIdentifier.Value);
            }
            else if (NamespaceIndex <= 255 && NumericIdentifier.HasValue && NumericIdentifier.Value <= 65535)
            {
                writer.WriteByte((byte)NodeIdEncoding.FourByte);
                writer.WriteByte((byte)NamespaceIndex);
                writer.WriteUInt16((ushort)NumericIdentifier.Value);
            }
            else if (NumericIdentifier.HasValue)
            {
                writer.WriteByte((byte)NodeIdEncoding.Numeric);
                writer.WriteUInt16(NamespaceIndex);
                writer.WriteUInt32(NumericIdentifier.Value);
            }
            else if (StringIdentifier != null)
            {
                writer.WriteByte((byte)NodeIdEncoding.String);
                writer.WriteUInt16(NamespaceIndex);
                writer.WriteString(StringIdentifier);
            }
            else
            {
                throw new NotImplementedException("Unsupported NodeId for encoding.");
            }
        }

        /// <summary>
        /// Decodes a NodeId using the provided OpcUaBinaryReader.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static NodeId Decode(OpcUaBinaryReader reader)
        {
            byte encodingByte = reader.ReadByte();
            bool hasNamespaceUri = (encodingByte & (byte)NodeIdEncoding.NamespaceUriFlag) != 0;
            bool hasServerIndex = (encodingByte & (byte)NodeIdEncoding.ServerIndexFlag) != 0;

            NodeIdEncoding format = (NodeIdEncoding)(encodingByte & (byte)NodeIdEncoding.TypeMask);

            NodeId node;

            switch (format)
            {
                case NodeIdEncoding.TwoByte:
                    node = new NodeId(0, (uint)reader.ReadByte());
                    break;

                case NodeIdEncoding.FourByte:
                    byte ns1 = reader.ReadByte();
                    ushort id1 = reader.ReadUInt16();
                    node = new NodeId(ns1, (uint)id1);
                    break;

                case NodeIdEncoding.Numeric:
                    ushort ns2 = reader.ReadUInt16();
                    uint id2 = reader.ReadUInt32();
                    node = new NodeId(ns2, id2);
                    break;

                case NodeIdEncoding.String:
                    ushort ns3 = reader.ReadUInt16();
                    string? s3 = reader.ReadString();
                    node = new NodeId(ns3, s3 ?? string.Empty);
                    break;

                case NodeIdEncoding.Guid:
                    ushort ns4 = reader.ReadUInt16();
                    Guid g4 = reader.ReadGuid();
                    node = new NodeId(ns4, g4);
                    break;

                case NodeIdEncoding.ByteString:
                    ushort ns5 = reader.ReadUInt16();
                    byte[]? b5 = reader.ReadByteString();
                    node = new NodeId(ns5, b5 ?? []);
                    break;

                default:
                    throw new Exception($"Unsupported NodeId encoding: {format}");
            }

            if (hasNamespaceUri)
            {
                node.NamespaceUri = reader.ReadString();
            }

            if (hasServerIndex)
            {
                node.ServerIndex = reader.ReadUInt32();
            }

            return node;
        }

        public override bool Equals(object? obj)
        {
            if (obj is NodeId other)
            {
                if (NamespaceIndex != other.NamespaceIndex) return false;

                if (NumericIdentifier.HasValue && other.NumericIdentifier.HasValue)
                    return NumericIdentifier.Value == other.NumericIdentifier.Value;

                if (StringIdentifier != null && other.StringIdentifier != null)
                    return StringIdentifier == other.StringIdentifier;

                if (GuidIdentifier.HasValue && other.GuidIdentifier.HasValue)
                    return GuidIdentifier.Value == other.GuidIdentifier.Value;

                if (ByteStringIdentifier != null && other.ByteStringIdentifier != null)
                    return ByteStringIdentifier.SequenceEqual(other.ByteStringIdentifier);

                return false;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = NamespaceIndex.GetHashCode();

            if (NumericIdentifier.HasValue) hash = HashCode.Combine(hash, NumericIdentifier.Value);
            else if (StringIdentifier != null) hash = HashCode.Combine(hash, StringIdentifier);
            else if (GuidIdentifier != null) hash = HashCode.Combine(hash, GuidIdentifier);
            else if (ByteStringIdentifier != null) hash = HashCode.Combine(hash, ByteStringIdentifier);

            return hash;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            // 1. Add Server Index (svr=) if it's an ExpandedNodeId component with a non-zero index
            if (ServerIndex > 0)
            {
                sb.Append($"svr={ServerIndex};");
            }

            // 2. Add Namespace
            // nsu= is used for NamespaceUri, ns= is used for NamespaceIndex
            if (!string.IsNullOrEmpty(NamespaceUri))
            {
                sb.Append($"nsu={NamespaceUri};");
            }
            else if (NamespaceIndex > 0)
            {
                sb.Append($"ns={NamespaceIndex};");
            }

            // 3. Identifier type and value
            if (NumericIdentifier.HasValue)
            {
                sb.Append($"i={NumericIdentifier.Value}");
            }
            else if (StringIdentifier != null)
            {
                sb.Append($"s={StringIdentifier}");
            }
            else if (GuidIdentifier.HasValue)
            {
                // GUIDs
                sb.Append($"g={GuidIdentifier.Value.ToString()}");
            }
            else if (ByteStringIdentifier != null)
            {
                // ByteStrings as Base64 strings
                sb.Append($"b={Convert.ToBase64String(ByteStringIdentifier)}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parses an OPC UA NodeId string (e.g., "ns=2;i=1234", "s=MyNode", "nsu=http://uri.com;g=00000000-0000-0000-0000-000000000000").
        /// </summary>
        /// <param name="input">The string representation of the NodeId.</param>
        /// <returns>A new NodeId instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        /// <exception cref="FormatException">Thrown when the string format is invalid.</exception>
        public static NodeId Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentNullException(nameof(input));

            if (!TryParse(input, out var result))
                throw new FormatException($"The string '{input}' is not a valid NodeId.");

            return result!;
        }

        /// <summary>
        /// Attempts to parse an OPC UA NodeId string.
        /// </summary>
        /// <param name="input">The string representation of the NodeId.</param>
        /// <param name="nodeId">The resulting NodeId if successful; otherwise null.</param>
        /// <returns>True if parsing succeeded, otherwise false.</returns>
        public static bool TryParse(string input, out NodeId? nodeId)
        {
            nodeId = null;
            if (string.IsNullOrWhiteSpace(input)) return false;

            ushort ns = 0;
            string? nsu = null;
            uint svr = 0;

            uint? numericId = null;
            string? stringId = null;
            Guid? guidId = null;
            byte[]? byteStringId = null;

            try
            {
                string[] parts = input.Split(';');
                if (parts.Length == 1 && uint.TryParse(parts[0], out uint rootId))
                {
                    nodeId = new NodeId(rootId);
                    return true;
                }

                foreach (string part in parts)
                {
                    string trimmedPart = part.Trim();
                    if (string.IsNullOrEmpty(trimmedPart)) continue;

                    int equalsIndex = trimmedPart.IndexOf('=');
                    if (equalsIndex == -1) continue;

                    string key = trimmedPart[..equalsIndex].Trim().ToLowerInvariant();
                    string value = trimmedPart[(equalsIndex + 1)..].Trim();

                    switch (key)
                    {
                        case "ns":
                            if (!ushort.TryParse(value, out ns)) return false;
                            break;

                        case "nsu":
                            nsu = value;
                            break;

                        case "svr":
                            if (!uint.TryParse(value, out svr)) return false;
                            break;

                        case "i":
                            if (!uint.TryParse(value, out uint i)) return false;
                            numericId = i;
                            break;

                        case "s":
                            stringId = value;
                            break;

                        case "g":
                            if (!Guid.TryParse(value, out Guid g)) return false;
                            guidId = g;
                            break;

                        case "b":
                            try { byteStringId = Convert.FromBase64String(value); }
                            catch { return false; }
                            break;
                    }
                }

                // Identifier selection logic
                if (numericId.HasValue)
                    nodeId = new NodeId(ns, numericId.Value);
                else if (stringId != null)
                    nodeId = new NodeId(ns, stringId);
                else if (guidId.HasValue)
                    nodeId = new NodeId(ns, guidId.Value);
                else if (byteStringId != null)
                    nodeId = new NodeId(ns, byteStringId);
                else
                    return false;

                if (!string.IsNullOrEmpty(nsu)) nodeId.NamespaceUri = nsu;
                if (svr > 0) nodeId.ServerIndex = svr;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}