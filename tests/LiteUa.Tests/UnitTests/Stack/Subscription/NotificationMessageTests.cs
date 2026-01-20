using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class NotificationMessageTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public NotificationMessageTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidData_SetsAllProperties()
        {
            // Arrange
            uint expectedSeq = 101;
            DateTime expectedTime = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(expectedSeq);
            _readerMock.Setup(r => r.ReadDateTime()).Returns(expectedTime);
            _readerMock.Setup(r => r.ReadInt32()).Returns(1);
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0)
                .Returns(0)
                .Returns(0);

            // Act
            var result = NotificationMessage.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(expectedSeq, result.SequenceNumber);
            Assert.Equal(expectedTime, result.PublishTime);
            Assert.NotNull(result.NotificationData);
            Assert.Single(result.NotificationData);
        }

        [Fact]
        public void Decode_KeepAliveMessage_ReturnsNullData()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(50u);
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.UtcNow);
            _readerMock.Setup(r => r.ReadInt32()).Returns(0);

            // Act
            var result = NotificationMessage.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(50u, result.SequenceNumber);
            Assert.Null(result.NotificationData);
        }

        [Fact]
        public void Decode_NullData_HandlesNegativeOne()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(1u);
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadInt32()).Returns(-1);

            // Act
            var result = NotificationMessage.Decode(_readerMock.Object);

            // Assert
            Assert.Null(result.NotificationData);
        }

        [Fact]
        public void Decode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() => {
                callOrder.Add("Sequence");
                return 0u;
            });

            _readerMock.Setup(r => r.ReadDateTime()).Returns(() => {
                callOrder.Add("Time");
                return DateTime.MinValue;
            });

            _readerMock.Setup(r => r.ReadInt32()).Returns(() => {
                callOrder.Add("Count");
                return 0;
            });

            // Act
            NotificationMessage.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("Sequence", callOrder[0]);
            Assert.Equal("Time", callOrder[1]);
            Assert.Equal("Count", callOrder[2]);
        }

        [Fact]
        public void Decode_MultipleNotifications_IteratesCorrectly()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(1u);
            _readerMock.Setup(r => r.ReadDateTime()).Returns(DateTime.MinValue);
            _readerMock.Setup(r => r.ReadInt32()).Returns(3);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Act
            var result = NotificationMessage.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(3, result.NotificationData!.Length);
            _readerMock.Verify(r => r.ReadByte(), Times.AtLeast(3));
        }
    }
}
