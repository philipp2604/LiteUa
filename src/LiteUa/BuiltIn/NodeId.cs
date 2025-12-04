using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// TODO: Fix documentation comments
/// TODO: Add unit tests
/// TODI: Add ToString() method

namespace LiteUa.BuiltIn
{
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

        public NodeId(uint id) { NamespaceIndex = 0; NumericIdentifier = id; }
        public NodeId(ushort ns, uint id) { NamespaceIndex = ns; NumericIdentifier = id; }
        public NodeId(ushort ns, string id) { NamespaceIndex = ns; StringIdentifier = id; }
        public NodeId(ushort ns, Guid id) { NamespaceIndex = ns; GuidIdentifier = id; }
        public NodeId(ushort ns, byte[] id) { NamespaceIndex = ns; ByteStringIdentifier = id; }

        public ushort NamespaceIndex { get; private set; }
        public uint? NumericIdentifier { get; private set; }
        public string? StringIdentifier { get; private set; }
        public Guid? GuidIdentifier { get; private set; }
        public byte[]? ByteStringIdentifier { get; private set; }
        public string? NamespaceUri { get; private set; }
        public uint ServerIndex { get; private set; }

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
                    node = new NodeId(ns5, 0)
                    {
                        ByteStringIdentifier = b5
                    };
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
    }
}
