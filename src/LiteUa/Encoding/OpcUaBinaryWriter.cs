using System.Buffers.Binary;

namespace LiteUa.Encoding
{
    /// <summary>
    /// Provides methods for writing OPC UA binary-encoded data types to a stream in little-endian format.
    /// </summary>
    /// <param name="stream">The output stream that which binary data is written to.</param>
    public class OpcUaBinaryWriter(Stream stream)
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
        /// Seeks to the specified position in the stream.
        /// </summary>
        /// <param name="offset">Position offset.</param>
        /// <param name="origin">Seek origin.</param>
        public void Seek(long offset, SeekOrigin origin)
        {
            _stream.Seek(offset, origin);
        }

        /// <summary>
        /// Writes a boolean value to the stream.
        /// </summary>
        /// <param name="value">The boolean value to write.</param>
        public virtual void WriteBoolean(bool value)
        {
            _stream.WriteByte((byte)(value ? 1 : 0));
        }

        /// <summary>
        /// Writes a byte value to the stream.
        /// </summary>
        /// <param name="value">The byte value to write.</param>
        public virtual void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        /// <summary>
        /// Writes multiple byte values to the stream.
        /// </summary>
        /// <param name="value">The byte values to write.</param>
        public virtual void WriteBytes(byte[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            _stream.Write(value, 0, value.Length);
        }

        /// <summary>
        /// Writes an Int16 value to the stream.
        /// </summary>
        /// <param name="value">The Int16 value to write.</param>
        public virtual void WriteInt16(short value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        /// <summary>
        /// Writes an UInt16 value to the stream.
        /// </summary>
        /// <param name="value">The UInt16 value to write.</param>
        public virtual void WriteUInt16(ushort value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        /// <summary>
        /// Writes an Int32 value to the stream.
        /// </summary>
        /// <param name="value">The Int32 value to write.</param>
        public virtual void WriteInt32(int value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        /// <summary>
        /// Writes an UInt32 value to the stream.
        /// </summary>
        /// <param name="value">The UInt32 value to write.</param>
        public virtual void WriteUInt32(uint value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        /// <summary>
        /// Writes an Int64 value to the stream.
        /// </summary>
        /// <param name="value">The Int64 value to write.</param>
        public virtual void WriteInt64(long value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        /// <summary>
        /// Writes an UInt64 value to the stream.
        /// </summary>
        /// <param name="value">The UInt64 value to write.</param>
        public virtual void WriteUInt64(ulong value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        /// <summary>
        /// Writes a float value to the stream.
        /// </summary>
        /// <param name="value">The float value to write.</param>
        public virtual void WriteFloat(float value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        /// <summary>
        /// Writes a double value to the stream.
        /// </summary>
        /// <param name="value">The double value to write.</param>
        public virtual void WriteDouble(double value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        /// <summary>
        /// Writes a string value to the stream.
        /// </summary>
        /// <param name="value">The string value to write.</param>
        public virtual void WriteString(string? value)
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

        /// <summary>
        /// Writes a byte string value to the stream.
        /// </summary>
        /// <param name="value">The byte string value to write.</param>
        public virtual void WriteByteString(byte[]? value)
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

        /// <summary>
        /// Writes a <see cref="DateTime"/> value to the stream.
        /// </summary>
        /// <param name="value">The <see cref="DateTime"/> value to write.</param>
        public virtual void WriteDateTime(DateTime value)
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

        /// <summary>
        /// Writes a Guid value to the stream.
        /// </summary>
        /// <param name="value">The Guid value to write.</param>
        public virtual void WriteGuid(Guid value)
        {
            byte[] guidBytes = value.ToByteArray();
            _stream.Write(guidBytes, 0, 16);
        }
    }
}