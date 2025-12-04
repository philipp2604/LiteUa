using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// TODO: Fix documentation comments
/// TODO: Add unit tests

namespace LiteUa.Encoding
{
    public class OpcUaBinaryWriter(Stream stream)
    {
        private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));

        public long Position => _stream.Position;
        public long Length => _stream.Length;

        public void Seek(long offset, SeekOrigin origin)
        {
            _stream.Seek(offset, origin);
        }

        public void WriteBoolean(bool value)
        {
            _stream.WriteByte((byte)(value ? 1 : 0));
        }

        public void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        public void WriteBytes(byte[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            _stream.Write(value, 0, value.Length);
        }

        public void WriteInt16(short value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        public void WriteUInt16(ushort value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        public void WriteInt32(int value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        public void WriteUInt32(uint value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        public void WriteInt64(long value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        public void WriteUInt64(ulong value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        public void WriteFloat(float value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        public void WriteDouble(double value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        public void WriteString(string? value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }

            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);
            WriteInt32(utf8Bytes.Length);

            if (utf8Bytes.Length > 0)
            {
                WriteBytes(utf8Bytes);
            }
        }

        public void WriteByteString(byte[]? value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            WriteInt32(value.Length);
            if (value.Length > 0)
            {
                WriteBytes(value);
            }
        }

        public void WriteDateTime(DateTime value)
        {
            if (value == DateTime.MinValue)
            {
                WriteInt64(0);
                return;
            }
            try
            {
                WriteInt64(value.ToFileTimeUtc());
            }
            catch
            {
                WriteInt64(0);
            }
        }

        public void WriteGuid(Guid value)
        {
            byte[] guidBytes = value.ToByteArray();
            _stream.Write(guidBytes, 0, 16);
        }
    }
}
