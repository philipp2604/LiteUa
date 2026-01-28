using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Method;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Method
{
    [Trait("Category", "Unit")]
    public class CallMethodRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public CallMethodRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(706u, CallMethodRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var objId = new NodeId(100);
            var methodId = new NodeId(200);
            var args = new[] { new Variant(1, BuiltInType.Int32) };

            // Act
            var request = new CallMethodRequest(objId, methodId, args);

            // Assert
            Assert.Equal(objId, request.ObjectId);
            Assert.Equal(methodId, request.MethodId);
            Assert.Equal(args, request.InputArguments);
        }

        [Fact]
        public void Encode_NullArguments_WritesCorrectSequence()
        {
            // Arrange
            var objId = new NodeId(0, 10u);    // TwoByte NodeId
            var methodId = new NodeId(0, 20u); // TwoByte NodeId
            var request = new CallMethodRequest(objId, methodId, null);

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteByte(0x00), Times.AtLeastOnce);
            _writerMock.Verify(w => w.WriteByte(10), Times.Once);
            _writerMock.Verify(w => w.WriteByte(20), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_WithArguments_WritesLengthAndElements()
        {
            // Arrange
            var objId = new NodeId(0);
            var methodId = new NodeId(0);
            var args = new[]
            {
            new Variant("Arg1", BuiltInType.String),
            new Variant(42, BuiltInType.Int32)
        };
            var request = new CallMethodRequest(objId, methodId, args);

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once);
            _writerMock.Verify(w => w.WriteString("Arg1"), Times.Once);
            _writerMock.Verify(w => w.WriteInt32(42), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new CallMethodRequest(
                new NodeId(0, 111u),
                new NodeId(0, 222u),
                null);

            var callOrder = new List<uint>();
            _writerMock.Setup(w => w.WriteByte(111))
                       .Callback(() => callOrder.Add(111));

            _writerMock.Setup(w => w.WriteByte(222))
                       .Callback(() => callOrder.Add(222));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            Assert.Equal(111u, callOrder[0]); // ObjectId first
            Assert.Equal(222u, callOrder[1]); // MethodId second
        }
    }
}