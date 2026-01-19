using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class DataChangeNotificationTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public DataChangeNotificationTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_BasicNotification_ParsesMonitoredItems()
        {
            // Arrange
            uint expectedHandle = 12345;

            // - MonitoredItems Count (1)
            // - DiagnosticInfos Count (0)
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(1)
                .Returns(0);

            // - ClientHandle (UInt32)
            // - DataValue Mask (Byte)
            _readerMock.Setup(r => r.ReadUInt32()).Returns(expectedHandle);
            _readerMock.Setup(r => r.ReadByte()).Returns(0x00); // Empty DataValue

            // Act
            var result = DataChangeNotification.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result.MonitoredItems);
            Assert.Single(result.MonitoredItems);
            Assert.Equal(expectedHandle, result.MonitoredItems[0]!.ClientHandle);
            Assert.Null(result.DiagnosticInfos);
        }

        [Fact]
        public void Decode_WithDiagnostics_ParsesBothArrays()
        {
            // Arrange
            // Counts: 1 item, 1 diagnostic
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(1)
                .Returns(1);
            _readerMock.Setup(r => r.ReadUInt32()).Returns(1u);
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0) // DataValue Mask
                .Returns(0); // Diagnostic Mask

            // Act
            var result = DataChangeNotification.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result.MonitoredItems);
            Assert.Single(result.MonitoredItems);
            Assert.NotNull(result.DiagnosticInfos);
            Assert.Single(result.DiagnosticInfos);
        }

        [Fact]
        public void Decode_EmptyArrays_ReturnsNullProperties()
        {
            // Arrange
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)
                .Returns(-1);

            // Act
            var result = DataChangeNotification.Decode(_readerMock.Object);

            // Assert
            Assert.Null(result.MonitoredItems);
            Assert.Null(result.DiagnosticInfos);
        }

        [Fact]
        public void Decode_VerifiesOrderOfExecution()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(() => {
                    callOrder.Add("ItemsCount");
                    return 0;
                })
                .Returns(() => {
                    callOrder.Add("DiagCount");
                    return 0;
                });

            // Act
            DataChangeNotification.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("ItemsCount", callOrder[0]);
            Assert.Equal("DiagCount", callOrder[1]);
        }

        [Fact]
        public void Decode_MultipleItems_LoopsCorrectly()
        {
            // Arrange
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(3) // 3 MonitoredItems
                .Returns(0); // 0 Diagnostics

            _readerMock.Setup(r => r.ReadUInt32()).Returns(1u);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Act
            var result = DataChangeNotification.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(3, result.MonitoredItems!.Length);
            _readerMock.Verify(r => r.ReadUInt32(), Times.Exactly(3));
        }
    }
}
