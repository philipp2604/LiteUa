using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// TODO: Add unit tests

namespace LiteUa.BuiltIn
{
    /// <summary>
    ///  Represents a Variant in OPC UA, which can hold a value of any built-in type, including arrays and multi-dimensional arrays.
    /// </summary>
    public class Variant
    {
        private const byte MaskDimensions = 0x40; // 64
        private const byte MaskArray = 0x80;      // 128
        private const byte MaskType = 0x3F;       // 63

        /// <summary>
        /// Gets or sets the value of the Variant.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="BuiltInType"/> of the Variant.
        /// </summary>
        public BuiltInType Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Variant represents an array.
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Gets the dimensions of the array if the Variant represents a multi-dimensional array.
        /// </summary>
        public int[]? ArrayDimensions { get; private set; }

        /// <summary>
        /// Creates a new instance of the <see cref="Variant"/> class.
        /// </summary>
        /// <param name="value">The variant's value.</param>
        /// <param name="type">The variant's <see cref="BuiltInType"/>.</param>
        public Variant(object? value, BuiltInType type)
        {
            Value = value;
            Type = type;

            if (value != null && value.GetType().IsArray)
            {
                if (value is byte[] && type == BuiltInType.ByteString)
                {
                    IsArray = false;
                }
                else if (value is byte[] && type == BuiltInType.Byte)
                {
                    IsArray = true;
                }
                else if (value is byte[])
                {
                    // Fallback
                    IsArray = false;
                }
                else
                {
                    IsArray = true;
                }
            }

        }

        /// <summary>
        /// Decodes a Variant using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded instance of <see cref="Variant"/>.</returns>
        public static Variant Decode(OpcUaBinaryReader reader)
        {
            byte encodingByte = reader.ReadByte();

            bool hasDimensions = (encodingByte & MaskDimensions) != 0; // Bit 6
            bool isArray = (encodingByte & MaskArray) != 0;            // Bit 7
            BuiltInType type = (BuiltInType)(encodingByte & MaskType);

            var v = new Variant(null, type)
            {
                IsArray = isArray
            };

            if (isArray)
            {
                // Array Decoding
                int length = reader.ReadInt32();
                if (length == -1) v.Value = null;
                else
                {
                    Array array = CreateArrayInstance(type, length);
                    for (int i = 0; i < length; i++)
                    {
                        object? element = ReadScalar(reader, type);
                        array.SetValue(element, i);
                    }
                    v.Value = array;
                }
            }
            else
            {
                // Scalar Decoding
                v.Value = ReadScalar(reader, type);
            }

            // Read dimensions if flag is set
            if (hasDimensions)
            {
                int dimCount = reader.ReadInt32();
                if (dimCount > 0)
                {
                    v.ArrayDimensions = new int[dimCount];
                    for (int i = 0; i < dimCount; i++) v.ArrayDimensions[i] = reader.ReadInt32();
                }
            }
            return v;
        }

        /// <summary>
        /// Encodes the Variant using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            byte encodingMask = (byte)Type;

            if (IsArray)
            {
                encodingMask |= MaskArray; // 0x80 -> Bit 7
            }

            if (ArrayDimensions != null)
            {
                encodingMask |= MaskDimensions; // 0x40 -> Bit 6
            }

            writer.WriteByte(encodingMask);

            if (IsArray)
            {
                var array = (Array?)Value;
                if (array == null)
                {
                    writer.WriteInt32(-1);
                }
                else
                {
                    writer.WriteInt32(array.Length);
                    foreach (var item in array)
                    {
                        Variant.WriteScalar(writer, item, Type);
                    }
                }
            }
            else
            {
                Variant.WriteScalar(writer, Value, Type);
            }

            if (ArrayDimensions != null)
            {
                writer.WriteInt32(ArrayDimensions.Length);
                foreach (int dim in ArrayDimensions) writer.WriteInt32(dim);
            }
        }

        private static Array CreateArrayInstance(BuiltInType type, int length)
        {
            return type switch
            {
                BuiltInType.Boolean => new bool[length],
                BuiltInType.SByte => new sbyte[length],
                BuiltInType.Byte => new byte[length],
                BuiltInType.Int16 => new short[length],
                BuiltInType.UInt16 => new ushort[length],
                BuiltInType.Int32 => new int[length],
                BuiltInType.UInt32 => new uint[length],
                BuiltInType.Int64 => new long[length],
                BuiltInType.UInt64 => new ulong[length],
                BuiltInType.Float => new float[length],
                BuiltInType.Double => new double[length],
                BuiltInType.String => new string[length],
                BuiltInType.DateTime => new DateTime[length],
                BuiltInType.Guid => new Guid[length],
                BuiltInType.ByteString => new byte[length][],
                BuiltInType.XmlElement => new byte[length][],
                BuiltInType.NodeId => new NodeId[length],
                BuiltInType.QualifiedName => new QualifiedName[length],
                BuiltInType.LocalizedText => new LocalizedText[length],
                BuiltInType.ExtensionObject => new ExtensionObject[length],
                BuiltInType.DataValue => new DataValue[length],
                BuiltInType.Variant => new Variant[length],
                BuiltInType.ExpandedNodeId => new ExpandedNodeId[length],
                BuiltInType.StatusCode => new StatusCode[length],
                BuiltInType.DiagnosticInfo => new DiagnosticInfo[length],
                _ => new object[length],// Fallback
            };
        }

        private static object? ReadScalar(OpcUaBinaryReader reader, BuiltInType type)
        {
            return type switch
            {
                BuiltInType.Null => null,
                BuiltInType.Boolean => reader.ReadBoolean(),
                BuiltInType.SByte => (sbyte)reader.ReadByte(),
                BuiltInType.Byte => reader.ReadByte(),
                BuiltInType.Int16 => reader.ReadInt16(),
                BuiltInType.UInt16 => reader.ReadUInt16(),
                BuiltInType.Int32 => reader.ReadInt32(),
                BuiltInType.UInt32 => reader.ReadUInt32(),
                BuiltInType.Int64 => reader.ReadInt64(),
                BuiltInType.UInt64 => reader.ReadUInt64(),
                BuiltInType.Float => reader.ReadFloat(),
                BuiltInType.Double => reader.ReadDouble(),
                BuiltInType.String => reader.ReadString(),
                BuiltInType.DateTime => reader.ReadDateTime(),
                BuiltInType.Guid => reader.ReadGuid(),
                BuiltInType.ByteString => reader.ReadByteString(),
                BuiltInType.XmlElement => reader.ReadByteString(),
                BuiltInType.NodeId => NodeId.Decode(reader),
                BuiltInType.StatusCode => StatusCode.Decode(reader),
                BuiltInType.QualifiedName => QualifiedName.Decode(reader),
                BuiltInType.LocalizedText => LocalizedText.Decode(reader),
                BuiltInType.ExtensionObject => ExtensionObject.Decode(reader),
                BuiltInType.DataValue => DataValue.Decode(reader),
                BuiltInType.Variant => Variant.Decode(reader),
                BuiltInType.ExpandedNodeId => ExpandedNodeId.Decode(reader),
                BuiltInType.DiagnosticInfo => DiagnosticInfo.Decode(reader),
                _ => throw new NotImplementedException($"Decoding for {type} not implemented."),
            };
        }

        private static void WriteScalar(OpcUaBinaryWriter writer, object? value, BuiltInType type)
        {
            if (value == null) return;

            switch (type)
            {
                case BuiltInType.Boolean: writer.WriteBoolean((bool)value); break;
                case BuiltInType.SByte: writer.WriteByte((byte)(sbyte)value); break;
                case BuiltInType.Byte: writer.WriteByte((byte)value); break;
                case BuiltInType.Int16: writer.WriteInt16((short)value); break;
                case BuiltInType.UInt16: writer.WriteUInt16((ushort)value); break;
                case BuiltInType.Int32: writer.WriteInt32((int)value); break;
                case BuiltInType.UInt32: writer.WriteUInt32((uint)value); break;
                case BuiltInType.Int64: writer.WriteInt64((long)value); break;
                case BuiltInType.UInt64: writer.WriteUInt64((ulong)value); break;
                case BuiltInType.Float: writer.WriteFloat((float)value); break;
                case BuiltInType.Double: writer.WriteDouble((double)value); break;
                case BuiltInType.String: writer.WriteString((string)value); break;
                case BuiltInType.DateTime: writer.WriteDateTime((DateTime)value); break;
                case BuiltInType.Guid: writer.WriteGuid((Guid)value); break;
                case BuiltInType.ByteString: writer.WriteByteString((byte[])value); break;
                case BuiltInType.XmlElement: writer.WriteByteString((byte[])value); break;
                case BuiltInType.NodeId: ((NodeId)value).Encode(writer); break;
                case BuiltInType.StatusCode: ((StatusCode)value).Encode(writer); break;
                case BuiltInType.QualifiedName: ((QualifiedName)value).Encode(writer); break;
                case BuiltInType.LocalizedText: ((LocalizedText)value).Encode(writer); break;
                case BuiltInType.ExtensionObject: ((ExtensionObject)value).Encode(writer); break;
                case BuiltInType.Null:
                    break;
                case BuiltInType.ExpandedNodeId: ((ExpandedNodeId)value).Encode(writer); break;
                case BuiltInType.DataValue: ((DataValue)value).Encode(writer); break;
                case BuiltInType.Variant: ((Variant)value).Encode(writer); break;
                case BuiltInType.DiagnosticInfo: ((DiagnosticInfo)value).Encode(writer); break;
                default: throw new NotImplementedException($"Encoding for {type} not implemented.");
            }
        }

        public override string ToString()
        {
            if (IsArray)
            {
                var arr = Value as Array;
                return $"{Type}[{arr?.Length ?? 0}]";
            }
            return $"{Type}: {Value}";
        }
    }
}
