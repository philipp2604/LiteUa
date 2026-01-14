using System.Buffers.Binary;

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

        /// <summary>
        /// Reads a boolean value from the stream.
        /// </summary>
        /// <returns>The retrieved boolean value.</returns>
        /// <exception cref="EndOfStreamException"></exception>
        public virtual bool ReadBoolean()
        {
            int val = _stream.ReadByte();
            if (val == -1) throw new EndOfStreamException();
            return val != 0;
        }

        /// <summary>
        /// Reads a byte value from the stream.
        /// </summary>
        /// <returns>The retrieved byte value.</returns>
        /// <exception cref="EndOfStreamException"></exception>
        public virtual byte ReadByte()
        {
            int val = _stream.ReadByte();
            if (val == -1) throw new EndOfStreamException();
            return (byte)val;
        }

        /// <summary>
        /// Reads multiple byte values from the stream.
        /// </summary>
        /// <returns>The retrieved byte values.</returns>
        /// <exception cref="EndOfStreamException"></exception>
        public virtual byte[] ReadBytes(int count)
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

        /// <summary>
        /// Reads an Int16 value from the stream.
        /// </summary>
        /// <returns>The retrieved Int16 value.</returns>
        /// <exception cref="EndOfStreamException"></exception>
        public virtual short ReadInt16()
        {
            Span<byte> buffer = stackalloc byte[2];
            ReadExact(buffer);
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        /// <summary>
        /// Reads an UInt16 value from the stream.
        /// </summary>
        /// <returns>The retrieved UInt16 value.</returns>
        public virtual ushort ReadUInt16()
        {
            Span<byte> buffer = stackalloc byte[2];
            ReadExact(buffer);
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        /// <summary>
        /// Reads an Int32 value from the stream.
        /// </summary>
        /// <returns>The retrieved Int32 value.</returns>
        public virtual int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[4];
            ReadExact(buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        /// <summary>
        /// Reads an UInt32 value from the stream.
        /// </summary>
        /// <returns>The retrieved UInt32 value.</returns>
        public virtual uint ReadUInt32()
        {
            Span<byte> buffer = stackalloc byte[4];
            ReadExact(buffer);
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        /// <summary>
        /// Reads an Int64 value from the stream.
        /// </summary>
        /// <returns>The retrieved Int64 value.</returns>
        public virtual long ReadInt64()
        {
            Span<byte> buffer = stackalloc byte[8];
            ReadExact(buffer);
            return BinaryPrimitives.ReadInt64LittleEndian(buffer);
        }

        /// <summary>
        /// Reads an UInt64 value from the stream.
        /// </summary>
        /// <returns>The retrieved UInt64 value.</returns>
        public virtual ulong ReadUInt64()
        {
            Span<byte> buffer = stackalloc byte[8];
            ReadExact(buffer);
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        /// <summary>
        /// Reads a float value from the stream.
        /// </summary>
        /// <returns>The retrieved float value.</returns>
        public virtual float ReadFloat()
        {
            Span<byte> buffer = stackalloc byte[4];
            ReadExact(buffer);
            return BinaryPrimitives.ReadSingleLittleEndian(buffer);
        }

        /// <summary>
        /// Reads a double value from the stream.
        /// </summary>
        /// <returns>The retrieved double value.</returns>
        public virtual double ReadDouble()
        {
            Span<byte> buffer = stackalloc byte[8];
            ReadExact(buffer);
            return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
        }

        /// <summary>
        /// Reads a string value from the stream.
        /// </summary>
        /// <returns>The retrieved string value.</returns>
        public virtual string? ReadString()
        {
            int length = ReadInt32();
            if (length == -1) return null;
            if (length == 0) return string.Empty;

            // Maybe add max length check

            byte[] bytes = ReadBytes(length);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Reads a <see cref="DateTime"/> value from the stream.
        /// </summary>
        /// <returns>The retrieved <see cref="DateTime"/> value.</returns>
        public virtual DateTime ReadDateTime()
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

        /// <summary>
        /// Reads a Guid value from the stream.
        /// </summary>
        /// <returns>The retrieved Guid value.</returns>
        public virtual Guid ReadGuid()
        {
            byte[] bytes = ReadBytes(16);
            return new Guid(bytes);
        }

        /// <summary>
        /// Reads a ByteString value from the stream.
        /// </summary>
        /// <returns>The retrieved ByteString value.</returns>
        public virtual byte[]? ReadByteString()
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