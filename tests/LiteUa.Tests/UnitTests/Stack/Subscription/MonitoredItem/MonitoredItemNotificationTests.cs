using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Subscription.MonitoredItem;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Subscription.MonitoredItem
{
    [Trait("Category", "Unit")]
    public class MonitoredItemNotificationTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public MonitoredItemNotificationTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidData_SetsProperties()
        {
            // Arrange
            uint expectedHandle = 42;
            // DataValue Mask: 0x01 (Value present)
            // Variant Type: 0x06 (Int32)
            // Variant Value: 100

            _readerMock.Setup(r => r.ReadUInt32()).Returns(expectedHandle);

            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0x01)  // DataValue Mask (Value Bit)
                .Returns(0x06); // Variant BuiltInType (Int32)

            _readerMock.Setup(r => r.ReadInt32()).Returns(100);

            // Act
            var result = MonitoredItemNotification.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(expectedHandle, result.ClientHandle);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.Value.Value);
            Assert.Equal(100, result.Value.Value.Value);
            Assert.Equal(BuiltInType.Int32, result.Value.Value.Type);
        }

        [Fact]
        public void Decode_EmptyDataValue_ParsesHandleAndDefaultValue()
        {
            // Arrange
            uint expectedHandle = 99;
            _readerMock.Setup(r => r.ReadUInt32()).Returns(expectedHandle);

            // DataValue Mask: 0x00 (Nothing present)
            _readerMock.Setup(r => r.ReadByte()).Returns(0x00);

            // Act
            var result = MonitoredItemNotification.Decode(_readerMock.Object);

            // Assert
            Assert.Equal(expectedHandle, result.ClientHandle);
            Assert.NotNull(result.Value);
            Assert.Null(result.Value.Value);
        }

        [Fact]
        public void Decode_VerifiesFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() =>
            {
                callOrder.Add("Handle");
                return 1u;
            });

            _readerMock.Setup(r => r.ReadByte()).Returns(() =>
            {
                callOrder.Add("ValueMask");
                return 0;
            });

            // Act
            MonitoredItemNotification.Decode(_readerMock.Object);

            // Assert
            // ClientHandle (UInt32) -> DataValue (starts with Mask/Byte)
            Assert.Equal("Handle", callOrder[0]);
            Assert.Equal("ValueMask", callOrder[1]);
        }
    }
}