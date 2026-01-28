using LiteUa.Encoding;
using LiteUa.Stack.Discovery;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Discovery
{
    [Trait("Category", "Unit")]
    public class GetEndpointsRequestTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public GetEndpointsRequestTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            // GetEndpointsRequest identifier is 428
            Assert.Equal(428u, GetEndpointsRequest.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Encode_NullValues_WritesCorrectNullMarkers()
        {
            // Arrange
            var request = new GetEndpointsRequest
            {
                EndpointUrl = null,
                LocaleIds = null,
                ProfileUris = null
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // 1. EndpointUrl as null string
            _writerMock.Verify(w => w.WriteString(null), Times.AtLeastOnce());

            // 2. LocaleIds as null array (-1)
            // 3. ProfileUris as null array (-1)
            _writerMock.Verify(w => w.WriteInt32(-1), Times.Exactly(2));
        }

        [Fact]
        public void Encode_FullRequest_WritesAllFields()
        {
            // Arrange
            var request = new GetEndpointsRequest
            {
                EndpointUrl = "opc.tcp://localhost",
                LocaleIds = ["en-US"],
                ProfileUris = ["urn:profile1", "urn:profile2"]
            };

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            // NodeId
            _writerMock.Verify(w => w.WriteUInt16(428), Times.Once);

            // EndpointUrl
            _writerMock.Verify(w => w.WriteString("opc.tcp://localhost"), Times.Once);

            // LocaleIds
            _writerMock.Verify(w => w.WriteInt32(1), Times.Once);
            _writerMock.Verify(w => w.WriteString("en-US"), Times.Once);

            // ProfileUris
            _writerMock.Verify(w => w.WriteInt32(2), Times.Once);
            _writerMock.Verify(w => w.WriteString("urn:profile1"), Times.Once);
            _writerMock.Verify(w => w.WriteString("urn:profile2"), Times.Once);
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var request = new GetEndpointsRequest
            {
                EndpointUrl = "sentinel-url",
                LocaleIds = ["sentinel-locale"],
                ProfileUris = ["sentinel-profile"]
            };

            var callOrder = new List<string>();

            _writerMock.Setup(w => w.WriteString("sentinel-url")).Callback(() => callOrder.Add("Url"));
            _writerMock.Setup(w => w.WriteString("sentinel-locale")).Callback(() => callOrder.Add("Locale"));
            _writerMock.Setup(w => w.WriteString("sentinel-profile")).Callback(() => callOrder.Add("Profile"));

            // Act
            request.Encode(_writerMock.Object);

            // Assert
            int urlIdx = callOrder.IndexOf("Url");
            int localeIdx = callOrder.IndexOf("Locale");
            int profileIdx = callOrder.IndexOf("Profile");

            Assert.True(urlIdx < localeIdx);
            Assert.True(localeIdx < profileIdx);
        }
    }
}