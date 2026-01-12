using LiteUa.BuiltIn;
using LiteUa.Encoding;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.BuiltIn
{
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
    }
}
