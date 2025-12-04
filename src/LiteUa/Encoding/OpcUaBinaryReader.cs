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
    /// <summary>
    /// Provides methods for reading OPC UA binary-encoded data types from a stream in little-endian format.
    /// </summary>
    /// <param name="stream">The input stream from which binary data is read.</param>
    public class OpcUaBinaryReader(Stream stream)
    {
        private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));

        /// <summary>
        /// Gets the current position within the stream.
        /// </summary>
        public long Position => _stream.Position;

        /// <summary>
        /// Gets the length of the stream.
        /// </summary>
        public long Length => _stream.Length;

        public bool ReadBoolean()
        {
            int val = _stream.ReadByte();
            if (val == -1) throw new EndOfStreamException();
            return val != 0;
        }

        public byte ReadByte()
        {
            int val = _stream.ReadByte();
            if (val == -1) throw new EndOfStreamException();
            return (byte)val;
        }

        public byte[] ReadBytes(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count == 0) return [];

            byte[] buffer = new byte[count];
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = _stream.Read(buffer, totalRead, count - totalRead);
                if (read == 0) throw new EndOfStreamException();
                totalRead += read;
            }
            return buffer;
        }

        public short ReadInt16()
        {
            Span<byte> buffer = stackalloc byte[2];
            ReadExact(buffer);
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        public ushort ReadUInt16()
        {
            Span<byte> buffer = stackalloc byte[2];
            ReadExact(buffer);
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        public int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[4];
            ReadExact(buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        public uint ReadUInt32()
        {
            Span<byte> buffer = stackalloc byte[4];
            ReadExact(buffer);
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        public long ReadInt64()
        {
            Span<byte> buffer = stackalloc byte[8];
            ReadExact(buffer);
            return BinaryPrimitives.ReadInt64LittleEndian(buffer);
        }

        public ulong ReadUInt64()
        {
            Span<byte> buffer = stackalloc byte[8];
            ReadExact(buffer);
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        public float ReadFloat()
        {
            Span<byte> buffer = stackalloc byte[4];
            ReadExact(buffer);
            return BinaryPrimitives.ReadSingleLittleEndian(buffer);
        }

        public double ReadDouble()
        {
            Span<byte> buffer = stackalloc byte[8];
            ReadExact(buffer);
            return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
        }

        public string? ReadString()
        {
            int length = ReadInt32();
            if (length == -1) return null;
            if (length == 0) return string.Empty;

            // Maybe add max length check

            byte[] bytes = ReadBytes(length);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public DateTime ReadDateTime()
        {
            long ticks = ReadInt64();
            if (ticks == 0) return DateTime.MinValue;
            try
            {
                return DateTime.FromFileTimeUtc(ticks);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public Guid ReadGuid()
        {
            byte[] bytes = ReadBytes(16);
            return new Guid(bytes);
        }

        public byte[]? ReadByteString()
        {
            int length = ReadInt32();
            if (length == -1) return null;
            if (length == 0) return [];

            return ReadBytes(length);
        }

        private void ReadExact(Span<byte> buffer)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int bytesRead = _stream.Read(buffer[totalRead..]);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("Unexpected end of stream");
                }
                totalRead += bytesRead;
            }
        }
    }
}
