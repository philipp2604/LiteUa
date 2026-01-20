using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Method;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Method
{
    [Trait("Category", "Unit")]
    public class CallRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public CallRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(712u, CallRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var methods = new[] { new CallMethodRequest(new NodeId(1), new NodeId(2)) };

            // Act
            var request = new CallRequest(methods);

            // Assert
            Assert.Same(methods, request.MethodsToCall);
            Assert.NotNull(request.RequestHeader);
        }

        [Fact]
        public void Encode_NullMethodsToCall_WritesNegativeOne()
        {
            // Arrange
            var request = new CallRequest(null!);

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // NodeId 712 written (TwoByte 0x00, 200... wait, 712 is > 255 so it uses FourByte 0x01)
            _writerMock.Verify(w => w.WriteUInt16(712), Times.Once);
            // Null array marker
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithMethods_WritesLengthAndDelegatesToElements()
        {
            // Arrange
            var methods = new[]
            {
            new CallMethodRequest(new NodeId(10), new NodeId(11)),
            new CallMethodRequest(new NodeId(20), new NodeId(21))
        };
            var request = new CallRequest(methods);

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt16(712), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once);
            _writerMock.Verify(w => w.WriteByte(10), Times.Once);
            _writerMock.Verify(w => w.WriteByte(20), Times.Once);
        }

        [Fact]
        public void Encode_SequenceCheck_VerifiesFieldOrder()
        {
            // Arrange
            var request = new CallRequest([new CallMethodRequest(new NodeId(0), new NodeId(0))]);
            var callOrder = new List<string>();

            _writerMock.Setup(w => w.WriteUInt16(712))
                       .Callback(() => callOrder.Add("ServiceID"));

            _writerMock.Setup(w => w.WriteInt32(1))
                       .Callback(() => callOrder.Add("ArrayLength"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int serviceIdx = callOrder.IndexOf("ServiceID");
            int arrayIdx = callOrder.IndexOf("ArrayLength");

            Assert.True(serviceIdx != -1);
            Assert.True(arrayIdx != -1);
            Assert.True(serviceIdx < arrayIdx);
        }
    }
}
