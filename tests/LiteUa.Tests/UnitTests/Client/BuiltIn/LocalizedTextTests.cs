using LiteUa.BuiltIn;
using LiteUa.Encoding;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Client.BuiltIn
{
    public class LocalizedTextTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public LocalizedTextTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        #region Decode Tests

        [Fact]
        public void Decode_MaskZero_ReturnsEmptyObject()
        {
            // Arrange: Mask 0x00 (No fields present)
            _readerMock.Setup(r => r.ReadByte()).Returns(0x00);

            // Act
            var result = LocalizedText.Decode(_readerMock.Object);

            // Assert
            Assert.Null(result.Locale);
            Assert.Null(result.Text);
            _readerMock.Verify(r => r.ReadString(), Times.Never);
        }

        [Fact]
        public void Decode_MaskThree_ReadsBothFields()
        {
            // Arrange: Mask 0x03 (Locale and Text present)
            _readerMock.Setup(r => r.ReadByte()).Returns(0x03);
            _readerMock.SetupSequence(r => r.ReadString())
                .Returns("en-US")
                .Returns("Hello World");

            // Act
            var result = LocalizedText.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("en-US", result.Locale);
            Assert.Equal("Hello World", result.Text);
        }

        [Theory]
        [InlineData(0x01, "en-US", null)] // Locale Only
        [InlineData(0x02, null, "Just Text")] // Text Only
        public void Decode_PartialMask_ReadsCorrectFields(byte mask, string? expectedLocale, string? expectedText)
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(mask);
            _readerMock.Setup(r => r.ReadString()).Returns(expectedLocale ?? expectedText!);

            // Act
            var result = LocalizedText.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(expectedLocale, result.Locale);
            Assert.Equal(expectedText, result.Text);
        }

        #endregion

        #region Encode Tests

        [Fact]
        public void Encode_EmptyObject_WritesMaskZero()
        {
            // Arrange
            var lt = new LocalizedText();

            // Act
            lt.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteByte(0x00), Times.Once);
            _writerMock.Verify(w => w.WriteString(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Encode_FullObject_WritesMaskThreeAndStrings()
        {
            // Arrange
            var lt = new LocalizedText { Locale = "de-DE", Text = "Hallo" };

            // Act
            lt.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteByte(0x03), Times.Once);
            _writerMock.Verify(w => w.WriteString("de-DE"), Times.Once);
            _writerMock.Verify(w => w.WriteString("Hallo"), Times.Once);
        }

        #endregion

        #region Operator Tests

        [Fact]
        public void ImplicitOperator_ToString_ReturnsText()
        {
            // Arrange
            var lt = new LocalizedText { Text = "Success" };

            // Act
            string result = lt;

            // Assert
            Assert.Equal("Success", result);
        }

        [Fact]
        public void ImplicitOperator_FromNull_ReturnsEmptyString()
        {
            // Arrange
            LocalizedText? lt = null;

            // Act
            string result = lt!; // Triggers implicit operator

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ImplicitOperator_FromString_SetsTextProperty()
        {
            // Arrange
            string rawText = "Auto-created";

            // Act
            LocalizedText lt = rawText;

            // Assert
            Assert.Equal("Auto-created", lt.Text);
            Assert.Null(lt.Locale);
        }

        #endregion
    }
}
