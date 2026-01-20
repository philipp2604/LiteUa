using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Session;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Session
{
    [Trait("Category", "Unit")]
    public class ActivateSessionRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public ActivateSessionRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(467u, ActivateSessionRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Encode_MissingUserIdentityToken_ThrowsException()
        {
            // Arrange
            var request = new ActivateSessionRequest { UserIdentityToken = null };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => request.Encode(_writerMock.Object));
            Assert.Equal("UserIdentityToken must be set", ex.Message);
        }

        [Fact]
        public void Encode_FullRequest_WritesCorrectSequence()
        {
            // Arrange
            var request = new ActivateSessionRequest
            {
                LocaleIds = ["en-US", "de-DE"],
                UserIdentityToken = new ExtensionObject { TypeId = new NodeId(321) }, // Anonymous
                ClientSignature = new SignatureData { Signature = [ 0xA ] },
                UserTokenSignature = new SignatureData { Signature = [0xB] }
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. Service ID
            _writerMock.Verify(w => w.WriteUInt16(467), Times.Once);

            // 2. ClientSignature
            _writerMock.Verify(w => w.WriteByteString(It.Is<byte[]>(b => b[0] == 0xA)), Times.Once);

            // 3. ClientSoftwareCertificates (Fixed 0)
            _writerMock.Verify(w => w.WriteInt32(0), Times.Once);

            // 4. LocaleIds
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once);
            _writerMock.Verify(w => w.WriteString("en-US"), Times.Once);
            _writerMock.Verify(w => w.WriteString("de-DE"), Times.Once);

            // 5. UserIdentityToken
            _writerMock.Verify(w => w.WriteUInt16(321), Times.Once);

            // 6. UserTokenSignature
            _writerMock.Verify(w => w.WriteByteString(It.Is<byte[]>(b => b[0] == 0xB)), Times.Once);
        }

        [Fact]
        public void Encode_NullLocaleIds_WritesNegativeOne()
        {
            // Arrange
            var request = new ActivateSessionRequest
            {
                LocaleIds = null,
                UserIdentityToken = new ExtensionObject()
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // Should write -1 for null array
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new ActivateSessionRequest
            {
                LocaleIds = ["locale-marker"],
                UserIdentityToken = new ExtensionObject { TypeId = new NodeId(999) }
            };

            var callOrder = new List<string>();
            _writerMock.Setup(w => w.WriteInt32(0)).Callback(() => callOrder.Add("SoftwareCerts"));
            _writerMock.Setup(w => w.WriteString("locale-marker")).Callback(() => callOrder.Add("LocaleIds"));
            _writerMock.Setup(w => w.WriteUInt16(999)).Callback(() => callOrder.Add("IdentityToken"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int softIdx = callOrder.IndexOf("SoftwareCerts");
            int localeIdx = callOrder.IndexOf("LocaleIds");
            int identityIdx = callOrder.IndexOf("IdentityToken");

            Assert.True(softIdx < localeIdx);
            Assert.True(localeIdx < identityIdx);
        }
    }
}