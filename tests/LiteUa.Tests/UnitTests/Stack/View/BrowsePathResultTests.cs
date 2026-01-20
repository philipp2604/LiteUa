using LiteUa.Encoding;
using LiteUa.Stack.View;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class BrowsePathResultTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public BrowsePathResultTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_SuccessWithTargets_ParsesCorrectly()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadInt32()).Returns(1);
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0);
            _readerMock.SetupSequence(r => r.ReadUInt32()).Returns(0u);

            // Act
            var result = BrowsePathResult.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.StatusCode.IsGood);
            Assert.NotNull(result.Targets);
            Assert.Single(result.Targets);
        }

        [Fact]
        public void Decode_NoTargets_ReturnsNullTargets()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);

            // Act
            var result = BrowsePathResult.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.StatusCode.IsGood);
            Assert.Null(result.Targets);
        }

        [Fact]
        public void Decode_NullArray_HandlesNegativeOne()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0x80010000u); // Bad_UnexpectedError
            _readerMock.Setup(r => r.ReadInt32()).Returns(-1);

            // Act
            var result = BrowsePathResult.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.StatusCode.IsBad);
            Assert.Null(result.Targets);
        }

        [Fact]
        public void Decode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() =>
            {
                callOrder.Add("StatusCode");
                return 0u;
            });

            _readerMock.Setup(r => r.ReadInt32()).Returns(() =>
            {
                callOrder.Add("TargetsCount");
                return 0;
            });

            // Act
            BrowsePathResult.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("StatusCode", callOrder[0]);
            Assert.Equal("TargetsCount", callOrder[1]);
        }

        [Fact]
        public void Decode_MultipleTargets_IteratesCorrectly()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadInt32()).Returns(3);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Act
            var result = BrowsePathResult.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(3, result.Targets!.Length);
            _readerMock.Verify(r => r.ReadUInt32(), Times.AtLeast(3));
        }
    }
}
