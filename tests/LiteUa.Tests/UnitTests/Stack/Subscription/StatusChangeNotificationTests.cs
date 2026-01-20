using LiteUa.Encoding;
using LiteUa.Stack.Subscription;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Subscription
{
    [Trait("Category", "Unit")]
    public class StatusChangeNotificationTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public StatusChangeNotificationTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_GoodStatus_NoDiagnostics_ParsesCorrectly()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.Setup(r => r.ReadByte()).Returns(0);

            // Act
            var result = StatusChangeNotification.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.Status.IsGood);
            Assert.Null(result.DiagnosticInfo);
        }

        [Fact]
        public void Decode_BadStatus_WithDiagnostics_ParsesBoth()
        {
            // Arrange
            // StatusCode (Bad_Timeout = 0x800A0000)
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0x800A0000u);

            // DiagnosticInfo Mask (0x01 = SymbolicId present)
            _readerMock.Setup(r => r.ReadByte()).Returns(0x01);
            // SymbolicId (Int32)
            _readerMock.Setup(r => r.ReadInt32()).Returns(123);

            // Act
            var result = StatusChangeNotification.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.Status.IsBad);
            Assert.NotNull(result.DiagnosticInfo);
            Assert.Equal(123, result.DiagnosticInfo.SymbolicId);
        }

        [Fact]
        public void Decode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() =>
            {
                callOrder.Add("Status");
                return 0u;
            });

            _readerMock.Setup(r => r.ReadByte()).Returns(() =>
            {
                callOrder.Add("DiagMask");
                return 0;
            });

            // Act
            StatusChangeNotification.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("Status", callOrder[0]);
            Assert.Equal("DiagMask", callOrder[1]);
        }
    }
}
