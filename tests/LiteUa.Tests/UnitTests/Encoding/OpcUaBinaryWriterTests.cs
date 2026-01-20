using LiteUa.Encoding;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Encoding
{
    [Trait("Category", "Unit")]
    public class OpcUaBinaryWriterTests
    {
        private static (MemoryStream, OpcUaBinaryWriter) CreateWriter()
        {
            var ms = new MemoryStream();
            return (ms, new OpcUaBinaryWriter(ms));
        }

        [Fact]
        public void Constructor_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OpcUaBinaryWriter(null!));
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public void WriteBoolean_WritesCorrectByte(bool input, byte expected)
        {
            var (ms, writer) = CreateWriter();
            writer.WriteBoolean(input);
            Assert.Equal(expected, ms.ToArray()[0]);
        }

        [Fact]
        public void WriteUInt16_WritesLittleEndian()
        {
            var (ms, writer) = CreateWriter();
            ushort value = 0x1234; // 4660

            writer.WriteUInt16(value);

            // Little Endian: 0x34 0x12
            Assert.Equal(new byte[] { 0x34, 0x12 }, ms.ToArray());
        }

        [Fact]
        public void WriteInt32_WritesLittleEndian()
        {
            var (ms, writer) = CreateWriter();
            int value = 0x12345678;

            writer.WriteInt32(value);

            // Little Endian: 78 56 34 12
            Assert.Equal(new byte[] { 0x78, 0x56, 0x34, 0x12 }, ms.ToArray());
        }

        [Fact]
        public void WriteString_Null_WritesNegativeOneLength()
        {
            var (ms, writer) = CreateWriter();
            writer.WriteString(null);

            // OPC UA Null String is represented by a -1 length (FF FF FF FF)
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, ms.ToArray());
        }

        [Fact]
        public void WriteString_Empty_WritesZeroLength()
        {
            var (ms, writer) = CreateWriter();
            writer.WriteString(string.Empty);

            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, ms.ToArray());
        }

        [Fact]
        public void WriteString_ValidText_WritesLengthAndUtf8Bytes()
        {
            var (ms, writer) = CreateWriter();
            string text = "OPC"; // 3 bytes

            writer.WriteString(text);

            byte[] result = ms.ToArray();
            Assert.Equal(3, BinaryPrimitives.ReadInt32LittleEndian(result.AsSpan(0, 4)));
            Assert.Equal("OPC", System.Text.Encoding.UTF8.GetString(result, 4, 3));
        }

        [Fact]
        public void WriteByteString_ValidData_WritesCorrectBytes()
        {
            var (ms, writer) = CreateWriter();
            byte[] data = [0xAA, 0xBB, 0xCC];

            writer.WriteByteString(data);

            byte[] result = ms.ToArray();
            Assert.Equal(3, BinaryPrimitives.ReadInt32LittleEndian(result.AsSpan(0, 4)));
            Assert.Equal(0xAA, result[4]);
            Assert.Equal(0xCC, result[6]);
        }

        [Fact]
        public void WriteDateTime_MinValue_WritesZero()
        {
            var (ms, writer) = CreateWriter();
            writer.WriteDateTime(DateTime.MinValue);

            // Should write 8 bytes of zeros
            Assert.Equal(new byte[8], ms.ToArray());
        }

        [Fact]
        public void WriteDateTime_ValidDate_WritesCorrectTicks()
        {
            var (ms, writer) = CreateWriter();
            DateTime date = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            long expectedTicks = date.ToFileTimeUtc();

            writer.WriteDateTime(date);

            byte[] result = ms.ToArray();
            Assert.Equal(expectedTicks, BinaryPrimitives.ReadInt64LittleEndian(result));
        }

        [Fact]
        public void WriteGuid_WritesSixteenBytes()
        {
            var (ms, writer) = CreateWriter();
            Guid guid = Guid.NewGuid();

            writer.WriteGuid(guid);

            Assert.Equal(guid.ToByteArray(), ms.ToArray());
        }

        [Fact]
        public void SeekAndPosition_InteractCorrectWithStream()
        {
            var (ms, writer) = CreateWriter();
            writer.WriteInt32(100);

            Assert.Equal(4, writer.Position);

            writer.Seek(0, SeekOrigin.Begin);
            Assert.Equal(0, writer.Position);

            writer.WriteInt32(200);
            ms.Position = 0;
            // Verify we overwrote the first int
            byte[] buffer = new byte[4];
            ms.ReadExactly(buffer);
            Assert.Equal(200, BinaryPrimitives.ReadInt32LittleEndian(buffer));
        }

        [Fact]
        public void WriteBytes_NullInput_ThrowsArgumentNullException()
        {
            var (_, writer) = CreateWriter();
            Assert.Throws<ArgumentNullException>(() => writer.WriteBytes(null!));
        }
    }
}
