using LiteUa.Encoding;
using LiteUa.Stack.Discovery;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Discovery
{
    [Trait("Category", "Unit")]
    public class ApplicationDescriptionTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public ApplicationDescriptionTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_FullDescription_SetsAllProperties()
        {
            // Arrange
            _readerMock.SetupSequence(r => r.ReadString())
                .Returns("urn:app:uri")    // ApplicationUri
                .Returns("urn:prod:uri")   // ProductUri
                                           // LocalizedText.Decode will call ReadByte for mask, setup below
                .Returns("LocalizedName")  // ApplicationName.Text (if mask bit 0x02 is set)
                .Returns("gw:uri")         // GatewayServerUri
                .Returns("profile:uri")    // DiscoveryProfileUri
                .Returns("http://url1");   // DiscoveryUrls[0]

            // LocalizedText mask: 0x02 (Text only)
            _readerMock.Setup(r => r.ReadByte()).Returns(0x02);

            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(1) // ApplicationType
                .Returns(1); // DiscoveryUrls Count

            // Act
            var result = ApplicationDescription.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("urn:app:uri", result.ApplicationUri);
            Assert.Equal("urn:prod:uri", result.ProductUri);
            Assert.Equal("LocalizedName", result.ApplicationName?.Text);
            Assert.Equal(1, (int)result.Type!);
            Assert.Equal("gw:uri", result.GatewayServerUri);
            Assert.Single(result.DiscoveryUrls!);
            Assert.Equal("http://url1", result.DiscoveryUrls![0]);
        }

        [Fact]
        public void Decode_EmptyDiscoveryUrls_ReturnsEmptyArray()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadString()).Returns("test");
            _readerMock.Setup(r => r.ReadByte()).Returns(0); // Empty LocalizedText

            // 1. Header
            // 2. ApplicationType
            // 3. DiscoveryUrls Count = 0
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(0);

            // Act
            var result = ApplicationDescription.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result.DiscoveryUrls);
            Assert.Empty(result.DiscoveryUrls);
        }

        [Fact]
        public void Decode_NullDiscoveryUrls_ReturnsNull()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadString()).Returns("test");
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Count = -1 (Null array in OPC UA)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(-1);

            // Act
            var result = ApplicationDescription.Decode(_readerMock.Object);

            // Assert
            Assert.Empty(result.DiscoveryUrls!);
        }

        [Fact]
        public void Decode_VerifiesCallSequence()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.SetupSequence(r => r.ReadString())
                .Returns(() => { callOrder.Add("AppUri"); return "a"; })
                .Returns(() => { callOrder.Add("ProdUri"); return "p"; })
                .Returns(() => { callOrder.Add("Gateway"); return "g"; })
                .Returns(() => { callOrder.Add("Profile"); return "pr"; });

            _readerMock.Setup(r => r.ReadByte()).Returns(0); // LocalizedText
            _readerMock.Setup(r => r.ReadInt32()).Returns(0); // Type & Count

            // Act
            ApplicationDescription.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("AppUri", callOrder[0]);
            Assert.Equal("ProdUri", callOrder[1]);
            Assert.Equal("Gateway", callOrder[2]);
            Assert.Equal("Profile", callOrder[3]);
        }
    }
}
