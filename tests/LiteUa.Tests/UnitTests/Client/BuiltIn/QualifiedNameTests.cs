using LiteUa.BuiltIn;
using LiteUa.Encoding;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Client.BuiltIn
{
    [Trait("Category", "Unit")]
    public class QualifiedNameTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public QualifiedNameTests()
        {
            // Initializing mocks with a dummy stream
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            ushort expectedNs = 4;
            string expectedName = "MachineStatus";

            // Act
            var qn = new QualifiedName(expectedNs, expectedName);

            // Assert
            Assert.Equal(expectedNs, qn.NamespaceIndex);
            Assert.Equal(expectedName, qn.Name);
        }

        [Fact]
        public void Encode_CallsWriterInCorrectOrder()
        {
            // Arrange
            var qn = new QualifiedName(2, "Temperature");

            // Act
            qn.Encode(_writerMock.Object);

            // Assert
            // 1. NamespaceIndex, 2. Name
            _writerMock.Verify(w => w.WriteUInt16(2), Times.Once);
            _writerMock.Verify(w => w.WriteString("Temperature"), Times.Once);
        }

        [Fact]
        public void Decode_ValidStream_ReturnsCorrectObject()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt16()).Returns(1);
            _readerMock.Setup(r => r.ReadString()).Returns("Voltage");

            // Act
            var result = QualifiedName.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.NamespaceIndex);
            Assert.Equal("Voltage", result.Name);
        }

        [Fact]
        public void Decode_NullNameInStream_ThrowsInvalidDataException()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt16()).Returns(1);
            _readerMock.Setup(r => r.ReadString()).Returns((string?)null); // Simulate null string from reader

            // Act & Assert
            var exception = Assert.Throws<InvalidDataException>(() => QualifiedName.Decode(_readerMock.Object));
            Assert.Equal("Name must not be null.", exception.Message);
        }

        [Theory]
        [InlineData(0, "Root", "0:Root")]
        [InlineData(15, "Signal", "15:Signal")]
        public void ToString_ReturnsFormattedString(ushort ns, string name, string expected)
        {
            // Arrange
            var qn = new QualifiedName(ns, name);

            // Act
            var result = qn.ToString();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Properties_AreMutable()
        {
            // Arrange
            var qn = new QualifiedName(1, "OldName");

            // Act
            qn.NamespaceIndex = 99;
            qn.Name = "NewName";

            // Assert
            Assert.Equal(99, qn.NamespaceIndex);
            Assert.Equal("NewName", qn.Name);
        }
    }
}
