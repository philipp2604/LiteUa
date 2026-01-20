using LiteUa.Encoding;
using LiteUa.Stack.Discovery;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Discovery
{
    [Trait("Category", "Unit")]
    public class GetEndpointsResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public GetEndpointsResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(431u, GetEndpointsResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_OneEndpoint_ParsesCorrectly()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadInt64()).Returns(0); // DateTime
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0); // Handle/Result
            _readerMock.Setup(r => r.ReadByte()).Returns(0); // Diagnostic Mask
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)  // Header
                .Returns(1)  // Endpoints Count
                .Returns(0)  // AppDesc Type
                .Returns(0)  // DiscoveryUrls
                .Returns(0)  // SecurityMode
                .Returns(0); // UserTokenCount
            _readerMock.Setup(r => r.ReadString()).Returns("dummy");
            _readerMock.Setup(r => r.ReadByteString()).Returns([]);

            // Act
            var response = new GetEndpointsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.Endpoints);
            Assert.Single(response.Endpoints);
            Assert.Equal("dummy", response.Endpoints[0].EndpointUrl);
        }

        [Fact]
        public void Decode_EmptyEndpoints_ReturnsEmptyArray()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadInt64()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Count = 0
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)  // Header
                .Returns(0); // Endpoints Count

            // Act
            var response = new GetEndpointsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.Endpoints);
            Assert.Empty(response.Endpoints);
        }

        [Fact]
        public void Decode_NullEndpoints_ReturnsEmptyArray()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadInt64()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Count = -1
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)  // Header
                .Returns(-1); // Endpoints Count

            // Act
            var response = new GetEndpointsResponse();
            response.Decode(_readerMock.Object);

            // Assert
            // Logic: if (count > 0) ... else Endpoints = []
            Assert.NotNull(response.Endpoints);
            Assert.Empty(response.Endpoints);
        }
    }
}
