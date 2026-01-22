using LiteUa.BuiltIn;
using LiteUa.Encoding;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.BuiltIn
{
    [Trait("Category", "Unit")]
    public class NodeIdTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public NodeIdTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        #region Constructor & Property Tests

        [Fact]
        public void Constructor_Numeric_SetsCorrectProperties()
        {
            var node = new NodeId(2, 1000u);
            Assert.Equal(2, node.NamespaceIndex);
            Assert.Equal(1000u, node.NumericIdentifier);
            Assert.Null(node.StringIdentifier);
        }

        [Fact]
        public void Constructor_String_SetsCorrectProperties()
        {
            var node = new NodeId(1, "MyNode");
            Assert.Equal(1, node.NamespaceIndex);
            Assert.Equal("MyNode", node.StringIdentifier);
            Assert.Null(node.NumericIdentifier);
        }

        #endregion

        #region Encode Tests

        [Fact]
        public void Encode_TwoByteNumeric_WritesCorrectBytes()
        {
            // Namespace 0, ID <= 255
            var node = new NodeId(0, 72u);

            node.Encode(_writerMock.Object);

            _writerMock.Verify(w => w.WriteByte(0x00), Times.Once); // TwoByte Flag
            _writerMock.Verify(w => w.WriteByte(72), Times.Once);   // Byte ID
        }

        [Fact]
        public void Encode_FourByteNumeric_WritesCorrectBytes()
        {
            // Namespace <= 255, ID <= 65535
            var node = new NodeId(5, 1024u);

            node.Encode(_writerMock.Object);

            _writerMock.Verify(w => w.WriteByte(0x01), Times.Once); // FourByte Flag
            _writerMock.Verify(w => w.WriteByte(5), Times.Once);    // NS Byte
            _writerMock.Verify(w => w.WriteUInt16(1024), Times.Once); // ID UInt16
        }

        [Fact]
        public void Encode_LargeNumeric_WritesStandardNumericFormat()
        {
            var node = new NodeId(300, 70000u);

            node.Encode(_writerMock.Object);

            _writerMock.Verify(w => w.WriteByte(0x02), Times.Once); // Numeric Flag
            _writerMock.Verify(w => w.WriteUInt16(300), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(70000u), Times.Once);
        }

        [Fact]
        public void Encode_StringIdentifier_WritesCorrectBytes()
        {
            var node = new NodeId(1, "MachineA");

            node.Encode(_writerMock.Object);

            _writerMock.Verify(w => w.WriteByte(0x03), Times.Once); // String Flag
            _writerMock.Verify(w => w.WriteUInt16(1), Times.Once);
            _writerMock.Verify(w => w.WriteString("MachineA"), Times.Once);
        }

        #endregion

        #region Decode Tests

        [Fact]
        public void Decode_TwoByte_ReturnsCorrectNode()
        {
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns((byte)0x00)
                .Returns((byte)42);

            var result = NodeId.Decode(_readerMock.Object);

            Assert.Equal(0, result.NamespaceIndex);
            Assert.Equal(42u, result.NumericIdentifier);
        }

        [Fact]
        public void Decode_ExpandedFlags_ReadsUriAndServerIndex()
        {
            // Mask: 0x80 (NS Uri) | 0x40 (Server Index) | 0x01 (FourByte) = 0xC1
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0xC1)
                .Returns(1); // NS
            _readerMock.Setup(r => r.ReadUInt16()).Returns(100); // ID
            _readerMock.Setup(r => r.ReadString()).Returns("http://opcfoundation.org/UA/");
            _readerMock.Setup(r => r.ReadUInt32()).Returns(5u);

            var result = NodeId.Decode(_readerMock.Object);

            Assert.Equal("http://opcfoundation.org/UA/", result.NamespaceUri);
            Assert.Equal(5u, result.ServerIndex);
            Assert.Equal(100u, result.NumericIdentifier);
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_SameValues_ReturnsTrue()
        {
            var node1 = new NodeId(1, "Test");
            var node2 = new NodeId(1, "Test");

            Assert.True(node1.Equals(node2));
            Assert.Equal(node1.GetHashCode(), node2.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentTypes_ReturnsFalse()
        {
            var node1 = new NodeId(1, 100u);
            var node2 = new NodeId(1, "100");

            Assert.False(node1.Equals(node2));
        }

        #endregion

        #region ToString Tests

        [Theory]
        [InlineData(0, 10u, "i=10")]
        [InlineData(1, 10u, "ns=1;i=10")]
        [InlineData(1, "MyNode", "ns=1;s=MyNode")]
        public void ToString_FormatsCorrectly(ushort ns, object id, string expected)
        {
            NodeId node = id is uint i ? new NodeId(ns, i) : new NodeId(ns, (string)id);
            Assert.Equal(expected, node.ToString());
        }

        [Fact]
        public void ToString_WithServerIndexAndUri_FormatsExpandedNodeId()
        {
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0xC0) // Flags for Svr and NSU, but TwoByte
                .Returns(1); // ID
            _readerMock.Setup(r => r.ReadString()).Returns("urn:myuri");
            _readerMock.Setup(r => r.ReadUInt32()).Returns(2u);

            var node = NodeId.Decode(_readerMock.Object);

            var result = node.ToString();

            Assert.Contains("svr=2;", result);
            Assert.Contains("nsu=urn:myuri;", result);
            Assert.Contains("i=1", result);
        }

        #endregion

        #region Parsing Tests
        [Theory]
        [InlineData("i=2254", 0, 2254u)]
        [InlineData("2254", 0, 2254u)] // Simple numeric fallback
        [InlineData("ns=4;i=1234", 4, 1234u)]
        [InlineData("ns=65535;i=4294967295", 65535, 4294967295u)] // Max values
        public void Parse_ValidNumericNodeId_ShouldSucceed(string input, ushort expectedNs, uint expectedId)
        {
            var result = NodeId.Parse(input);

            Assert.Equal(expectedNs, result.NamespaceIndex);
            Assert.Equal(expectedId, result.NumericIdentifier);
            Assert.Null(result.StringIdentifier);
        }

        [Theory]
        [InlineData("s=MyIdentifier", 0, "MyIdentifier")]
        [InlineData("ns=2;s=Device.Signal_1", 2, "Device.Signal_1")]
        [InlineData("ns=1;s=Complex=ID;With;Chars", 1, "Complex=ID")] // Note: splitting by ';' means the string ID ends at the first ';'
        public void Parse_ValidStringNodeId_ShouldSucceed(string input, ushort expectedNs, string expectedId)
        {
            var result = NodeId.Parse(input);

            Assert.Equal(expectedNs, result.NamespaceIndex);
            Assert.Equal(expectedId, result.StringIdentifier);
            Assert.Null(result.NumericIdentifier);
        }

        [Fact]
        public void Parse_ValidGuidNodeId_ShouldSucceed()
        {
            string guidStr = "72962b91-3254-463e-8974-90994f06553c";
            string input = $"ns=10;g={guidStr}";

            var result = NodeId.Parse(input);

            Assert.Equal(10, result.NamespaceIndex);
            Assert.Equal(new Guid(guidStr), result.GuidIdentifier);
        }

        [Fact]
        public void Parse_ValidByteStringNodeId_ShouldSucceed()
        {
            // "LiteUa" in Base64 is "TGl0ZVVh"
            string input = "ns=1;b=TGl0ZVVh";
            byte[] expected = [0x4C, 0x69, 0x74, 0x65, 0x55, 0x61]; // LiteUa

            var result = NodeId.Parse(input);

            Assert.Equal(1, result.NamespaceIndex);
            Assert.Equal(expected, result.ByteStringIdentifier);
        }

        [Fact]
        public void Parse_ExpandedNodeId_ShouldIncludeUriAndServerIndex()
        {
            string input = "svr=2;nsu=http://my-uri.com/ua;i=1000";

            var result = NodeId.Parse(input);

            Assert.Equal(2u, result.ServerIndex);
            Assert.Equal("http://my-uri.com/ua", result.NamespaceUri);
            Assert.Equal(1000u, result.NumericIdentifier);
            Assert.Equal(0, result.NamespaceIndex); // ns= was not provided, defaults to 0
        }

        [Theory]
        [InlineData("   ns=1;i=123   ")] // Leading/trailing whitespace
        [InlineData("NS=1;I=123")]       // Uppercase keys
        [InlineData("ns = 1 ; i = 123")] // Spaces around equals/semicolon
        public void Parse_FormattingVariations_ShouldBeRobust(string input)
        {
            var result = NodeId.Parse(input);

            Assert.Equal(1, result.NamespaceIndex);
            Assert.Equal(123u, result.NumericIdentifier);
        }

        [Theory]
        [InlineData("")]                // Empty
        [InlineData("ns=abc;i=123")]    // Non-numeric NS
        [InlineData("ns=1;i=abc")]      // Non-numeric ID
        [InlineData("ns=1")]            // Missing identifier part
        [InlineData("g=not-a-guid")]    // Invalid Guid
        [InlineData("b=!!!notbase64")]  // Invalid Base64
        public void TryParse_InvalidInput_ShouldReturnFalse(string input)
        {
            bool success = NodeId.TryParse(input, out var result);

            Assert.False(success);
            Assert.Null(result);
        }

        [Fact]
        public void Parse_NullInput_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => NodeId.Parse(null!));
        }

        [Fact]
        public void Parse_InvalidFormat_ShouldThrowFormatException()
        {
            Assert.Throws<FormatException>(() => NodeId.Parse("completely-invalid-string"));
        }

        [Fact]
        public void Parse_RoundTrip_ToString_Then_Parse_ShouldBeEqual()
        {
            // Arrange
            var original = new NodeId(2, "MyCustomString");

            // Act
            string toString = original.ToString();
            var parsed = NodeId.Parse(toString);

            // Assert
            Assert.Equal(original.NamespaceIndex, parsed.NamespaceIndex);
            Assert.Equal(original.StringIdentifier, parsed.StringIdentifier);
            Assert.Equal(original, parsed); // Uses your Equals override
        }
        #endregion
    }
}
