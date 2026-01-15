using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Subscription.MonitoredItem;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription.MonitoredItem
{
    [Trait("Category", "Unit")]
    public class MonitoringParametersTests
    {
        private readonly Mock<OpcUaBinaryWriter> _writerMock;

        public MonitoringParametersTests()
        {
            _writerMock = new Mock<OpcUaBinaryWriter>(new MemoryStream());
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var paramsObj = new MonitoringParameters();

            // Assert
            Assert.Equal(-1.0, paramsObj.SamplingInterval);
            Assert.Equal(0u, paramsObj.QueueSize);
            Assert.True(paramsObj.DiscardOldest);
            Assert.Null(paramsObj.Filter);
        }

        [Fact]
        public void Encode_FullParameters_WritesCorrectSequence()
        {
            // Arrange
            var paramsObj = new MonitoringParameters
            {
                ClientHandle = 42,
                SamplingInterval = 500.0,
                QueueSize = 10,
                DiscardOldest = false,
                Filter = new ExtensionObject { TypeId = new NodeId(0, 999u), Encoding = 0x01, Body = new byte[] { 0x1 } }
            };

            // Act
            paramsObj.Encode(_writerMock.Object);

            // Assert
            // 1. ClientHandle
            _writerMock.Verify(w => w.WriteUInt32(42u), Times.Once);

            // 2. SamplingInterval
            _writerMock.Verify(w => w.WriteDouble(500.0), Times.Once);

            // 3. Filter
            _writerMock.Verify(w => w.WriteUInt16(999), Times.Once);

            // 4. QueueSize
            _writerMock.Verify(w => w.WriteUInt32(10u), Times.Once);

            // 5. DiscardOldest
            _writerMock.Verify(w => w.WriteBoolean(false), Times.Once);
        }

        [Fact]
        public void Encode_NullFilter_EncodesEmptyExtensionObject()
        {
            // Arrange
            var paramsObj = new MonitoringParameters { Filter = null };

            // Act
            paramsObj.Encode(_writerMock.Object);

            // Assert
            _writerMock.Verify(w => w.WriteByte(0x00), Times.Exactly(3));
        }

        [Fact]
        public void Encode_VerifiesFieldOrder()
        {
            // Arrange
            var paramsObj = new MonitoringParameters
            {
                ClientHandle = 111,
                SamplingInterval = 222.2,
                QueueSize = 333,
                DiscardOldest = true
            };

            var callOrder = new List<string>();

            _writerMock.Setup(w => w.WriteUInt32(111)).Callback(() => callOrder.Add("Handle"));
            _writerMock.Setup(w => w.WriteDouble(222.2)).Callback(() => callOrder.Add("Interval"));
            _writerMock.Setup(w => w.WriteUInt32(333)).Callback(() => callOrder.Add("Queue"));
            _writerMock.Setup(w => w.WriteBoolean(true)).Callback(() => callOrder.Add("Discard"));

            // Act
            paramsObj.Encode(_writerMock.Object);

            // Assert
            // Handle -> Interval -> (Filter logic) -> Queue -> Discard
            Assert.Equal("Handle", callOrder[0]);
            Assert.Equal("Interval", callOrder[1]);
            Assert.Equal("Queue", callOrder[2]);
            Assert.Equal("Discard", callOrder[3]);
        }
    }
}
