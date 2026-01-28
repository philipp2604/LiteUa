using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Attribute
{
    [Trait("Category", "Unit")]
    public class ReadRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public ReadRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Encode_WithNodes_WritesCorrectValues()
        {
            // Arrange
            double uniqueMaxAge = 12345.67;
            uint uniqueTimestampsValue = 99;

            var request = new ReadRequest
            {
                NodesToRead = [new ReadValueId(new(10001))],
                MaxAge = uniqueMaxAge,
                TimestampsToReturn = (TimestampsToReturn)uniqueTimestampsValue
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. Verify NodeId for ReadRequest (631)
            _writerMock.Verify(w => w.WriteUInt16(631), Times.Once);

            // 2. Verify MaxAge field
            _writerMock.Verify(w => w.WriteDouble(uniqueMaxAge), Times.Once);

            // 3. Verify TimestampsToReturn
            _writerMock.Verify(w => w.WriteUInt32(uniqueTimestampsValue), Times.Once);

            // 4. Verify Array Length
            _writerMock.Verify(w => w.WriteInt32(1), Times.Once);
        }

        [Fact]
        public void Encode_SequenceCheck_VerifiesFieldOrder()
        {
            // Arrange
            var request = new ReadRequest
            {
                MaxAge = 777.7,
                TimestampsToReturn = (TimestampsToReturn)88
            };

            var callOrder = new List<string>();

            _writerMock.Setup(w => w.WriteDouble(777.7))
                       .Callback(() => callOrder.Add("MaxAge"));

            _writerMock.Setup(w => w.WriteUInt32(88))
                       .Callback(() => callOrder.Add("Timestamps"));

            _writerMock.Setup(w => w.WriteInt32(It.IsAny<int>()))
                       .Callback(() => callOrder.Add("ArrayLength"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int maxAgeIdx = callOrder.IndexOf("MaxAge");
            int timestampsIdx = callOrder.IndexOf("Timestamps");
            int arrayIdx = callOrder.LastIndexOf("ArrayLength");

            Assert.True(maxAgeIdx != -1);
            Assert.True(timestampsIdx != -1);
            Assert.True(maxAgeIdx < timestampsIdx);
            Assert.True(timestampsIdx < arrayIdx);
        }

        [Fact]
        public void Encode_NullNodes_WritesNegativeOne()
        {
            // Arrange
            var request = new ReadRequest { NodesToRead = null };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // OPC UA encodes null arrays as -1
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }
    }
}