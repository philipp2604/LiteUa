using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class PublishResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public PublishResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(829u, PublishResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_FullResponse_SetsAllProperties()
        {
            // Arrange
            var now = DateTime.UtcNow;
            _readerMock.Setup(r => r.ReadDateTime()).Returns(now);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header
                .Returns(2) // AvailableSequenceNumbers
                .Returns(0) // NotificationMessage inner array
                .Returns(1) // Results
                .Returns(1); // Diagnostics
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)    // Header Handle
                .Returns(0u)    // Header ServiceResult
                .Returns(55u)   // SubscriptionId
                .Returns(100u)  // AvailSeq[0]
                .Returns(101u)  // AvailSeq[1]
                .Returns(5u)    // NotificationMessage.SequenceNumber
                .Returns(0u);   // Results[0] (StatusCode Good)

            _readerMock.Setup(r => r.ReadBoolean()).Returns(true); // MoreNotifications
            _readerMock.Setup(r => r.Position).Returns(0);
            _readerMock.Setup(r => r.Length).Returns(1000);

            // Act
            var response = new PublishResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(55u, response.SubscriptionId);
            Assert.Equal(2, response.AvailableSequenceNumbers!.Length);
            Assert.True(response.MoreNotifications);
            Assert.NotNull(response.NotificationMessage);
            Assert.Equal(5u, response.NotificationMessage.SequenceNumber);
            Assert.Single(response.Results!);
            Assert.Single(response.DiagnosticInfos!);
        }

        [Fact]
        public void Decode_EmptyArrays_InitializesAvailableSequenceNumbersAsEmpty()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);

            // Counts: Header(0), AvailSeq(0), Notif(0), Results(0)
            _readerMock.SetupSequence(r => r.ReadInt32()).Returns(0);

            _readerMock.Setup(r => r.Position).Returns(100);
            _readerMock.Setup(r => r.Length).Returns(100); // Block diagnostics

            // Act
            var response = new PublishResponse();
            response.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(response.AvailableSequenceNumbers);
            Assert.Empty(response.AvailableSequenceNumbers);
            Assert.Null(response.Results);
        }

        [Fact]
        public void Decode_SequenceCheck_VerifiesFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() => {
                callOrder.Add("SubId");
                return 1u;
            });

            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0) // Header StringTable
                .Returns(() => {
                    callOrder.Add("AvailSeqCount");
                    return 0;
                })
                .Returns(0) // NotificationMessage Count
                .Returns(() => {
                    callOrder.Add("ResultsCount");
                    return 0;
                });

            _readerMock.Setup(r => r.ReadBoolean()).Returns(() => {
                callOrder.Add("MoreNotif");
                return false;
            });
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.Position).Returns(10);
            _readerMock.Setup(r => r.Length).Returns(10);

            // Act
            new PublishResponse().Decode(_readerMock.Object);

            // SubId -> AvailSeqCount -> MoreNotif -> NotificationMessage -> ResultsCount
            int subIdx = callOrder.IndexOf("SubId");
            int seqIdx = callOrder.IndexOf("AvailSeqCount");
            int moreIdx = callOrder.IndexOf("MoreNotif");
            int resIdx = callOrder.IndexOf("ResultsCount");

            Assert.True(subIdx < seqIdx);
            Assert.True(seqIdx < moreIdx);
            Assert.True(moreIdx < resIdx);
        }

        [Fact]
        public void Decode_PositionAtEnd_SkipsDiagnostics()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);

            // Skip diagnostics
            _readerMock.Setup(r => r.Position).Returns(50);
            _readerMock.Setup(r => r.Length).Returns(50);

            // Act
            var response = new PublishResponse();
            response.Decode(_readerMock.Object);

            // Assert
            _readerMock.Verify(r => r.ReadInt32(), Times.Exactly(4));
            Assert.Null(response.DiagnosticInfos);
        }
    }
}
