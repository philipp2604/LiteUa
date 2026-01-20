using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class CreateSubscriptionResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public CreateSubscriptionResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(790u, CreateSubscriptionResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_StandardResponse_SetsAllRevisedProperties()
        {
            // Arrange
            uint expectedSubId = 12345;
            double expectedInterval = 500.0;
            uint expectedLifetime = 60;
            uint expectedKeepAlive = 20;

            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.UtcNow);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u); // Handle/Result
            _readerMock.Setup(r => r.ReadByte()).Returns(0);   // DiagMask/ExtraMask
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);  // StringTable Count
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)             // Header Handle
                .Returns(0u)             // Header Result
                .Returns(expectedSubId)  // SubscriptionId
                .Returns(expectedLifetime) // RevisedLifetimeCount
                .Returns(expectedKeepAlive); // RevisedMaxKeepAliveCount

            _readerMock.Setup(r => r.ReadDouble()).Returns(expectedInterval);

            // don't enter DiagnosticInfo block (Position == Length)
            _readerMock.Setup(r => r.Position).Returns(100);
            _readerMock.Setup(r => r.Length).Returns(100);

            // Act
            var response = new CreateSubscriptionResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.ResponseHeader);
            Assert.Equal(expectedSubId, response.SubscriptionId);
            Assert.Equal(expectedInterval, response.RevisedPublishingInterval);
            Assert.Equal(expectedLifetime, response.RevisedLifetimeCount);
            Assert.Equal(expectedKeepAlive, response.RevisedMaxKeepAliveCount);
            Assert.Null(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_WithDiagnostics_ParsesDiagnosticArray()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0);

            // 1. Header StringTable (0)
            // 2. Diagnostic Count (1)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header
                .Returns(1); // Diagnostics Count

            _readerMock.Setup(r => r.ReadDouble()).Returns(0.0);

            // trigger the diag block
            _readerMock.Setup(r => r.Position).Returns(0);
            _readerMock.Setup(r => r.Length).Returns(500);

            // Act
            var response = new CreateSubscriptionResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.DiagnosticInfos);
            Assert.Single(response.DiagnosticInfos);
        }

        [Fact]
        public void Decode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadDateTime()).Returns(() => {
                callOrder.Add("HeaderTime");
                return DateTime.MinValue;
            });

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() => {
                callOrder.Add("UInt32");
                return 0u;
            });

            _readerMock.Setup(r => r.ReadDouble()).Returns(() => {
                callOrder.Add("Double");
                return 0.0;
            });

            // Header StringTable
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // skip diagnostics
            _readerMock.Setup(r => r.Position).Returns(100);
            _readerMock.Setup(r => r.Length).Returns(100);

            // Act
            var response = new CreateSubscriptionResponse();
            response.Decode(_readerMock.Object);

            // Header -> SubId (UInt32) -> Interval (Double) -> Lifetime (UInt32) -> KeepAlive (UInt32)

            Assert.Equal("HeaderTime", callOrder[0]);
            int firstUintIdx = callOrder.IndexOf("UInt32", 1);
            int doubleIdx = callOrder.IndexOf("Double");
            int lastUintIdx = callOrder.LastIndexOf("UInt32");

            Assert.True(firstUintIdx < doubleIdx);
            Assert.True(doubleIdx < lastUintIdx);
        }

        [Fact]
        public void Decode_HandlesPartialPacket_DoesNotTryToReadDiagnostics()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadDouble()).Returns(0);
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);

            _readerMock.Setup(r => r.Position).Returns(64);
            _readerMock.Setup(r => r.Length).Returns(64);

            // Act
            var response = new CreateSubscriptionResponse();
            response.Decode(_readerMock.Object);

            // Assert
            _readerMock.Verify(r => r.ReadInt32(), Times.Exactly(1));
            Assert.Null(response.DiagnosticInfos);
        }
    }
}
