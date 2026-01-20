using LiteUa.Encoding;
using LiteUa.Stack.SecureChannel;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.SecureChannel
{
    [Trait("Category", "Unit")]
    public class CloseSecureChannelRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public CloseSecureChannelRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(452u, CloseSecureChannelRequest.NodeId.NumericIdentifier);
            Assert.Equal(0, CloseSecureChannelRequest.NodeId.NamespaceIndex);
        }

        [Fact]
        public void Encode_WritesCorrectTypeIdentifier()
        {
            // Arrange
            var request = new CloseSecureChannelRequest();

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt16(452), Times.Once);
        }

        [Fact]
        public void Encode_CallsRequestHeaderEncode()
        {
            // Arrange
            var request = new CloseSecureChannelRequest();

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteUInt32(It.IsAny<uint>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new CloseSecureChannelRequest();
            var callOrder = new List<string>();

            _writerMock.Setup(w => w.WriteUInt16(452))
                       .Callback(() => callOrder.Add("TypeID"));

            _writerMock.Setup(w => w.WriteDateTime(It.IsAny<System.DateTime>()))
                       .Callback(() => callOrder.Add("HeaderTimestamp"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int typeIdx = callOrder.IndexOf("TypeID");
            int headerIdx = callOrder.IndexOf("HeaderTimestamp");

            Assert.True(typeIdx != -1);
            Assert.True(headerIdx != -1);
            Assert.True(typeIdx < headerIdx);
        }
    }
}
