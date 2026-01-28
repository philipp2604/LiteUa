using LiteUa.Encoding;
using System.Buffers.Binary;

namespace LiteUa.Tests.UnitTests.Encoding
{
    [Trait("Category", "Unit")]
    public class OpcUaBinaryReaderTests
    {
        private static OpcUaBinaryReader CreateReader(byte[] data)
        {
            return new OpcUaBinaryReader(new MemoryStream(data));
        }

        [Fact]
        public void Constructor_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OpcUaBinaryReader(null!));
        }

        [Theory]
        [InlineData(0x00, false)]
        [InlineData(0x01, true)]
        [InlineData(0xFF, true)]
        public void ReadBoolean_ReturnsCorrectValue(byte input, bool expected)
        {
            var reader = CreateReader([input]);
            Assert.Equal(expected, reader.ReadBoolean());
        }

        [Fact]
        public void ReadUInt16_LittleEndian_ReturnsCorrectValue()
        {
            // 0x01 0x00 in Little Endian is 1
            var reader = CreateReader([0x01, 0x00]);
            Assert.Equal((ushort)1, reader.ReadUInt16());

            // 0xFF 0x7F is 32767
            reader = CreateReader([0xFF, 0x7F]);
            Assert.Equal((ushort)32767, reader.ReadUInt16());
        }

        [Fact]
        public void ReadInt32_LittleEndian_ReturnsCorrectValue()
        {
            // 0x01 0x02 0x03 0x04 -> 0x04030201 (67305985)
            var reader = CreateReader([0x01, 0x02, 0x03, 0x04]);
            Assert.Equal(67305985, reader.ReadInt32());
        }

        [Fact]
        public void ReadFloat_ReturnsCorrectValue()
        {
            // 1.0f in IEEE 754 Little Endian: 0x00 0x00 0x80 0x3F
            var reader = CreateReader([0x00, 0x00, 0x80, 0x3F]);
            Assert.Equal(1.0f, reader.ReadFloat());
        }

        [Fact]
        public void ReadString_Null_ReturnsNull()
        {
            // Length -1 (0xFFFFFFFF) indicates a null string in OPC UA
            var reader = CreateReader([0xFF, 0xFF, 0xFF, 0xFF]);
            Assert.Null(reader.ReadString());
        }

        [Fact]
        public void ReadString_Empty_ReturnsEmptyString()
        {
            // Length 0
            var reader = CreateReader([0x00, 0x00, 0x00, 0x00]);
            Assert.Equal(string.Empty, reader.ReadString());
        }

        [Fact]
        public void ReadString_ValidData_ReturnsString()
        {
            // Length 4 ("Test") + UTF8 Bytes
            byte[] data = [0x04, 0x00, 0x00, 0x00, (byte)'T', (byte)'e', (byte)'s', (byte)'t'];
            var reader = CreateReader(data);
            Assert.Equal("Test", reader.ReadString());
        }

        [Fact]
        public void ReadDateTime_ValidTicks_ReturnsCorrectTime()
        {
            // Jan 1, 2000, 00:00:00 UTC
            DateTime testTime = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long ticks = testTime.ToFileTimeUtc();

            byte[] buffer = new byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, ticks);

            var reader = CreateReader(buffer);
            Assert.Equal(testTime, reader.ReadDateTime());
        }

        [Fact]
        public void ReadGuid_ReturnsCorrectGuid()
        {
            Guid expected = Guid.NewGuid();
            var reader = CreateReader(expected.ToByteArray());
            Assert.Equal(expected, reader.ReadGuid());
        }

        [Fact]
        public void ReadByteString_Null_ReturnsNull()
        {
            // Length -1
            var reader = CreateReader([0xFF, 0xFF, 0xFF, 0xFF]);
            Assert.Null(reader.ReadByteString());
        }

        [Fact]
        public void ReadByteString_ValidData_ReturnsBytes()
        {
            // Length 2 + data
            var reader = CreateReader([0x02, 0x00, 0x00, 0x00, 0xAA, 0xBB]);
            var result = reader.ReadByteString();
            Assert.Equal([0xAA, 0xBB], result);
        }

        [Fact]
        public void ReadBytes_CountZero_ReturnsEmptyArray()
        {
            var reader = CreateReader([]);
            Assert.Empty(reader.ReadBytes(0));
        }

        [Fact]
        public void ReadInt32_StreamTruncated_ThrowsEndOfStreamException()
        {
            // Only 2 bytes provided but 4 expected for Int32
            var reader = CreateReader([0x01, 0x02]);
            Assert.Throws<EndOfStreamException>(() => reader.ReadInt32());
        }

        [Fact]
        public void ReadBytes_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            var reader = CreateReader([]);
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadBytes(-1));
        }
    }
}