using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.Session;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Session
{
    [Trait("Category", "Unit")]
    public class ClientDescriptionTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public ClientDescriptionTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Encode_FullDescription_WritesCorrectSequence()
        {
            // Arrange
            var desc = new ClientDescription
            {
                ApplicationUri = "urn:client",
                ProductUri = "urn:prod",
                ApplicationName = new LocalizedText { Text = "MyClient" },
                Type = ApplicationType.Client,
                GatewayServerUri = "urn:gw",
                DiscoveryProfileUri = "urn:profile",
                DiscoveryUrls = ["url1", "url2"]
            };

            // Act
            desc.Encode(_writerMock.Object);

            // Assert
            // 1. Scalar Strings
            _writerMock.Verify(w => w.WriteString("urn:client"), Times.Once);
            _writerMock.Verify(w => w.WriteString("urn:prod"), Times.Once);
            _writerMock.Verify(w => w.WriteString("urn:gw"), Times.Once);
            _writerMock.Verify(w => w.WriteString("urn:profile"), Times.Once);

            // 2. LocalizedText (Mask 0x02 for Text presence)
            _writerMock.Verify(w => w.WriteByte(0x02), Times.Once);
            _writerMock.Verify(w => w.WriteString("MyClient"), Times.Once);

            // 3. ApplicationType (Int32)
            _writerMock.Verify(w => w.WriteInt32((int)ApplicationType.Client), Times.Once);

            // 4. DiscoveryUrls Array
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once); // Length
            _writerMock.Verify(w => w.WriteString("url1"), Times.Once);
            _writerMock.Verify(w => w.WriteString("url2"), Times.Once);
        }

        [Fact]
        public void Encode_NullApplicationName_EncodesEmptyLocalizedText()
        {
            // Arrange
            var desc = new ClientDescription { ApplicationName = null };

            // Act
            desc.Encode(_writerMock.Object);

            // Assert
            // An empty LocalizedText writes a mask byte of 0
            _writerMock.Verify(w => w.WriteByte(0x00), Times.Once);
        }

        [Fact]
        public void Encode_NullDiscoveryUrls_WritesNegativeOne()
        {
            // Arrange
            var desc = new ClientDescription { DiscoveryUrls = null };

            // Act
            desc.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var desc = new ClientDescription
            {
                ApplicationUri = "order-1",
                Type = (ApplicationType)77,
                GatewayServerUri = "order-2"
            };

            var callOrder = new List<string>();

            _writerMock.Setup(w => w.WriteString("order-1")).Callback(() => callOrder.Add("Uri"));
            _writerMock.Setup(w => w.WriteInt32(77)).Callback(() => callOrder.Add("Type"));
            _writerMock.Setup(w => w.WriteString("order-2")).Callback(() => callOrder.Add("Gateway"));

            // Act
            desc.Encode(_writerMock.Object);

            // Assert
            int uriIdx = callOrder.IndexOf("Uri");
            int typeIdx = callOrder.IndexOf("Type");
            int gwIdx = callOrder.IndexOf("Gateway");

            Assert.True(uriIdx < typeIdx);
            Assert.True(typeIdx < gwIdx);
        }
    }
}