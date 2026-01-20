using LiteUa.Encoding;
using LiteUa.Stack.Session;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Session
{
    [Trait("Category", "Unit")]
    public class CloseSessionRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public CloseSessionRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(473u, CloseSessionRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var request = new CloseSessionRequest();

            // Assert
            Assert.True(request.DeleteSubscriptions);
            Assert.NotNull(request.RequestHeader);
        }

        [Fact]
        public void Encode_WritesCorrectValues()
        {
            // Arrange
            var request = new CloseSessionRequest { DeleteSubscriptions = false };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt16(473), Times.Once);
            _writerMock.Verify(w => w.WriteBoolean(false), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new CloseSessionRequest { DeleteSubscriptions = true };
            var callOrder = new List<string>();

            // Track order: TypeID -> Header (indirectly) -> Boolean
            _writerMock.Setup(w => w.WriteUInt16(473))
                       .Callback(() => callOrder.Add("TypeID"));

            _writerMock.Setup(w => w.WriteBoolean(It.IsAny<bool>()))
                       .Callback(() => callOrder.Add("DeleteFlag"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int typeIdx = callOrder.IndexOf("TypeID");
            int flagIdx = callOrder.IndexOf("DeleteFlag");

            Assert.True(typeIdx != -1);
            Assert.True(flagIdx != -1);
            Assert.True(typeIdx < flagIdx);
        }
    }
}
