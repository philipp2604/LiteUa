using LiteUa.Encoding;
using LiteUa.Stack.Method;
using Moq;

namespace LiteUa.Tests.UnitTests.Stack.Method
{
    [Trait("Category", "Unit")]
    public class CallMethodResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public CallMethodResponseTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            Assert.Equal(709u, CallMethodResponse.NodeId.NumericIdentifier);
        }

        [Fact]
        public void Decode_EmptyArrays_ReturnsCorrectObject()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(0)  // InputArgumentResults
                .Returns(0)  // InputArgumentDiagnosticInfos
                .Returns(0); // OutputArguments

            // Act
            var result = CallMethodResponse.Decode(_readerMock.Object);

            // Assert
            Assert.True(result.StatusCode.IsGood);
            Assert.Null(result.InputArgumentResults);
            Assert.Null(result.InputArgumentDiagnosticInfos);
            Assert.Null(result.OutputArguments);
        }

        [Fact]
        public void Decode_PopulatedArrays_ParsesAllElements()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(1)  // Input Results Count
                .Returns(1)  // Diags Count
                .Returns(1); // Output Args Count
            _readerMock.SetupSequence(r => r.ReadUInt32())
                .Returns(0u)
                .Returns(0u);
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0)
                .Returns(0);

            // Act
            var result = CallMethodResponse.Decode(_readerMock.Object);

            // Assert
            Assert.Single(result.InputArgumentResults!);
            Assert.Single(result.InputArgumentDiagnosticInfos!);
            Assert.Single(result.OutputArguments!);
        }

        [Fact]
        public void Decode_HandlesNullArrays_WhenCountIsNegativeOne()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(-1)
                .Returns(-1)
                .Returns(-1);

            // Act
            var result = CallMethodResponse.Decode(_readerMock.Object);

            // Assert
            Assert.Null(result.InputArgumentResults);
            Assert.Null(result.InputArgumentDiagnosticInfos);
            Assert.Null(result.OutputArguments);
        }

        [Fact]
        public void Decode_SequenceCheck_VerifiesOrderOfArrays()
        {
            // Arrange
            var callOrder = new System.Collections.Generic.List<string>();

            _readerMock.Setup(r => r.ReadUInt32()).Returns(0u);
            _readerMock.SetupSequence(r => r.ReadInt32())
                .Returns(() => { callOrder.Add("ResultsCount"); return 0; })
                .Returns(() => { callOrder.Add("DiagsCount"); return 0; })
                .Returns(() => { callOrder.Add("OutputsCount"); return 0; });

            // Act
            CallMethodResponse.Decode(_readerMock.Object);

            // Assert
            Assert.Equal("ResultsCount", callOrder[0]);
            Assert.Equal("DiagsCount", callOrder[1]);
            Assert.Equal("OutputsCount", callOrder[2]);
        }
    }
}