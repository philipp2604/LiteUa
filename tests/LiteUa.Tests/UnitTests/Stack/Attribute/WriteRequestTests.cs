using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Attribute
{
    [Trait("Category", "Unit")]
    public class WriteRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public WriteRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            // OPC UA WriteRequest has the numeric identifier 673
            Assert.Equal(673u, WriteRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Encode_NullNodesToWrite_WritesNegativeOne()
        {
            // Arrange
            var request = new WriteRequest
            {
                NodesToWrite = null
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // OPC UA encodes null arrays as -1
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithNodes_WritesCorrectSequence()
        {
            // Arrange
            var writeValue = new WriteValue(new(100), new() { Value = new(1, BuiltInType.Int32) });

            var request = new WriteRequest
            {
                NodesToWrite = [writeValue]
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. Request Type NodeId (673)
            _writerMock.Verify(w => w.WriteUInt16(673), Times.Once);

            // 2. Array Length (1)
            _writerMock.Verify(w => w.WriteInt32(1), Times.Exactly(2)); // 1 for the header

            // 3. AttributeId
            _writerMock.Verify(w => w.WriteUInt32(13), Times.Once);
        }

        [Fact]
        public void Encode_SequenceCheck_VerifiesFieldOrder()
        {
            // Arrange
            var request = new WriteRequest
            {
                NodesToWrite = [new WriteValue(new(100), new() { Value = new(1, BuiltInType.Int32) }) { AttributeId = 666 }]
            };

            var callOrder = new List<string>();

            // Track order: Header starts -> TypeID -> Array length
            _writerMock.Setup(w => w.WriteUInt16(673))
                       .Callback(() => callOrder.Add("TypeID"));

            _writerMock.Setup(w => w.WriteInt32(1))
                       .Callback(() => callOrder.Add("ArrayLength"));
            _writerMock.Setup(w => w.WriteUInt32(666))
                   .Callback(() => callOrder.Add("AttributeID"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int typeIdx = callOrder.IndexOf("TypeID");
            int arrayIdx = callOrder.IndexOf("ArrayLength");
            int attrIdx = callOrder.IndexOf("AttributeID");

            Assert.True(typeIdx < arrayIdx);
            Assert.True(arrayIdx < attrIdx);
        }
    }
}
