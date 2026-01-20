using LiteUa.Encoding;
using LiteUa.Stack.Session;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Session
{
    [Trait("Category", "Unit")]
    public class SignatureDataTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public SignatureDataTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void StaticNull_HasNullProperties()
        {
            // Act & Assert
            Assert.Null(SignatureData.Null.Algorithm);
            Assert.Null(SignatureData.Null.Signature);
        }

        [Fact]
        public void Encode_ValidData_WritesCorrectSequence()
        {
            // Arrange
            var signature = new SignatureData
            {
                Algorithm = "http://www.w3.org/2000/09/xmldsig#rsa-sha1",
                Signature = [0x1, 0x2, 0x3, 0x4]
            };

            // Act
            signature.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteString("http://www.w3.org/2000/09/xmldsig#rsa-sha1"), Times.Once);
            _writerMock.Verify(w => w.WriteByteString(It.Is<byte[]>(b => b.Length == 4)), Times.Once);
        }

        [Fact]
        public void Encode_NullValues_CallsWriterWithNull()
        {
            // Arrange
            var signature = new SignatureData
            {
                Algorithm = null,
                Signature = null
            };

            // Act
            signature.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteString(null), Times.Once);
            _writerMock.Verify(w => w.WriteByteString(null), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var signature = new SignatureData
            {
                Algorithm = "algo-marker",
                Signature = [0xFF]
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteString("algo-marker"))
                       .Callback(() => callOrder.Add("Algorithm"));

            _writerMock.Setup(w => w.WriteByteString(It.IsAny<byte[]>()))
                       .Callback(() => callOrder.Add("Signature"));

            // Act
            signature.Encode(_writerMock.Object);

            // Assert
            Assert.Equal("Algorithm", callOrder[0]);
            Assert.Equal("Signature", callOrder[1]);
        }
    }
}
