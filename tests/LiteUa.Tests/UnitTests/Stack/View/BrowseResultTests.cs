using LiteUa.Encoding;
using LiteUa.Stack.View;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class BrowseResultTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public BrowseResultTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidStream_SetsAllProperties()
        {
            // Arrange
            var expectedContinuationPoint = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)    // StatusCode
                .Returns(0u);   // NodeId (inside ReferenceDescription)

            // ContinuationPoint (ByteString)
            _readerMock.Setup(r => r.ReadByteString()).Returns(expectedContinuationPoint);
            // References Count = 1
            _readerMock.Setup(r => r.ReadInt32()).Returns(1);

            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0) // NodeId TwoByte type
                .Returns(0) // NodeId Identifier
                .Returns(0); // QualifiedName Mask (Empty)

            _readerMock.Setup(r => r.ReadString()).Returns("ValidName");

            // Act
            var result = BrowseResult.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.StatusCode.IsGood);
            Assert.Equal(expectedContinuationPoint, result.ContinuationPoint);
            Assert.NotNull(result.References);
            Assert.Single(result.References);
        }

        [Fact]
        public void Decode_NoReferences_ReturnsNullReferences()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u); // Good
            _readerMock.Setup(r => r.ReadByteString()).Returns((byte[]?)null); // No Point
            _readerMock.Setup(r => r.ReadInt32()).Returns(0); // Count 0

            // Act
            var result = BrowseResult.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.StatusCode.IsGood);
            Assert.Null(result.ContinuationPoint);
            Assert.Null(result.References);
        }

        [Fact]
        public void Decode_HandlesNullArray_WhenCountIsNegativeOne()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadByteString()).Returns([]);
            _readerMock.Setup(r => r.ReadInt32()).Returns(-1);

            // Act
            var result = BrowseResult.Decode(_readerMock.Object);

            // Assert
            Assert.Null(result.References);
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
            _readerMock.Setup(r => r.ReadByteString()).Returns(() =>
            {
                callOrder.Add("ContPoint");
                return null;
            });
            _readerMock.Setup(r => r.ReadInt32()).Returns(() =>
            {
                callOrder.Add("RefsCount");
                return 0;
            });

            // Act
            BrowseResult.Decode(_readerMock.Object);

            // Assert
            // Code -> Point -> Count
            Assert.Equal("StatusCode", callOrder[0]);
            Assert.Equal("ContPoint", callOrder[1]);
            Assert.Equal("RefsCount", callOrder[2]);
        }

        [Fact]
        public void Decode_MultipleReferences_IteratesCorrectly()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadByteString()).Returns([]);
            _readerMock.Setup(r => r.ReadInt32()).Returns(3); // 3 References
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadString()).Returns("test");

            // Act
            var result = BrowseResult.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(3, result.References!.Length);
            _readerMock.Verify(r => r.ReadByte(), Times.AtLeast(3));
        }
    }
}
