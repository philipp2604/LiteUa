using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Attribute
{
    public class WriteValueTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public WriteValueTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_SetsPropertiesAndDefaults()
        {
            // Arrange
            var nodeId = new NodeId(100);
            var dataValue = new DataValue { Value = new Variant(10, BuiltInType.Int32) };

            // Act
            var wv = new WriteValue(nodeId, dataValue);

            // Assert
            Assert.Equal(nodeId, wv.NodeId);
            Assert.Equal(dataValue, wv.Value);
            Assert.Equal(13u, wv.AttributeId); // Default should be 13 (Value)
            Assert.Null(wv.IndexRange);
        }

        [Fact]
        public void Constructor_NullArguments_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WriteValue(null!, new DataValue()));
            Assert.Throws<ArgumentNullException>(() => new WriteValue(new NodeId(0), null!));
        }

        [Fact]
        public void Encode_WritesCorrectValuesInSequence()
        {
            // Arrange
            var nodeId = new NodeId(1, "MyNode");
            var dataValue = new DataValue { StatusCode = new StatusCode(0x80000000) };
            var request = new WriteValue(nodeId, dataValue)
            {
                AttributeId = 42,
                IndexRange = "2:5"
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. NodeId (String type 0x03)
            _writerMock.Verify(w => w.WriteByte(0x03), Times.Once);
            _writerMock.Verify(w => w.WriteString("MyNode"), Times.Once);

            // 2. AttributeId
            _writerMock.Verify(w => w.WriteUInt32(42u), Times.Once);

            // 3. IndexRange
            _writerMock.Verify(w => w.WriteString("2:5"), Times.Once);

            // 4. DataValue (Mask 0x02 for StatusCode)
            _writerMock.Verify(w => w.WriteByte(0x02), Times.Once);
            _writerMock.Verify(w => w.WriteUInt32(0x80000000), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var wv = new WriteValue(new NodeId(0), new DataValue())
            {
                AttributeId = 999,
                IndexRange = "range-marker"
            };

            var callOrder = new List<string>();

            // Tracking relative order of fields
            _writerMock.Setup(w => w.WriteUInt32(999))
                       .Callback(() => callOrder.Add("Attr"));

            _writerMock.Setup(w => w.WriteString("range-marker"))
                       .Callback(() => callOrder.Add("Range"));

            _writerMock.Setup(w => w.WriteByte(It.IsAny<byte>()))
                       .Callback((byte b) => {
                           if (callOrder.Contains("Range")) callOrder.Add("DataValueStart");
                       });

            // Act
            wv.Encode(_writerMock.Object);

            // Assert
            int attrIdx = callOrder.IndexOf("Attr");
            int rangeIdx = callOrder.IndexOf("Range");
            int dvIdx = callOrder.IndexOf("DataValueStart");

            Assert.True(attrIdx < rangeIdx, "AttributeId must be before IndexRange");
            Assert.True(rangeIdx < dvIdx, "IndexRange must be before DataValue");
        }
    }
}
