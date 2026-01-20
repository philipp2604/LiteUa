using LiteUa.BuiltIn;
using LiteUa.Stack.Method;
using LiteUa.Transport;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Transport
{
    [Trait("Category", "Unit")]
    public class UaTcpClientChannelExtensionsTests
    {
        private readonly Mock<IUaTcpClientChannel> _channelMock;
        private readonly NodeId _objId = new(0, 100u);
        private readonly NodeId _methId = new(0, 200u);

        public UaTcpClientChannelExtensionsTests()
        {
            _channelMock = new Mock<IUaTcpClientChannel>();
        }

        [Fact]
        public async Task CallTypedAsync_MapsScalarsAndVerifiesOrder()
        {
            // Arrange
            var input = new SimpleInput { Id = 500, Name = "TestDevice" };
            _channelMock.Setup(c => c.CallAsync(_objId, _methId, It.IsAny<CancellationToken>(), It.IsAny<Variant[]>()))
                .ReturnsAsync([new Variant(true, BuiltInType.Boolean)])
                .Callback<NodeId, NodeId, CancellationToken, Variant[]>((obj, meth, ct, vars) =>
                {
                    Assert.Equal(2, vars.Length);
                    Assert.Equal(500, vars[0].Value); // Order 0
                    Assert.Equal("TestDevice", vars[1].Value); // Order 1
                });

            // Act
            var result = await _channelMock.Object.CallTypedAsync<SimpleInput, SimpleOutput>(_objId, _methId, input);

            // Assert
            Assert.True(result.Success);
            _channelMock.Verify(c => c.CallAsync(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<CancellationToken>(), It.IsAny<Variant[]>()), Times.Once);
        }

        [Fact]
        public async Task CallTypedAsync_MapsExtensionObject_Input()
        {
            // Arrange
            var complex = new ComplexData { Val = "Hello" };
            var input = new ComplexInput { Data = complex };

            _channelMock.Setup(c => c.CallAsync(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<CancellationToken>(), It.IsAny<Variant[]>()))
                .ReturnsAsync([])
                .Callback<NodeId, NodeId, CancellationToken, Variant[]>((o, m, ct, vars) =>
                {
                    var extObj = Assert.IsType<ExtensionObject>(vars[0].Value);
                    Assert.Same(complex, extObj.DecodedValue);
                });

            // Act
            await _channelMock.Object.CallTypedAsync<ComplexInput, object>(_objId, _methId, input);
        }

        [Fact]
        public async Task CallTypedAsync_MapsArrays_InputAndOutput()
        {
            // Arrange
            var input = new ArrayInput { Numbers = [1, 2, 3] };
#pragma warning disable CA1861
            var outputVariants = new[] {
                new Variant(new[] { 10, 20 }, BuiltInType.Int32, isArray: true)
            };
#pragma warning restore CA1861

            _channelMock.Setup(c => c.CallAsync(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<CancellationToken>(), It.IsAny<Variant[]>()))
                .ReturnsAsync(outputVariants);

            // Act
            var result = await _channelMock.Object.CallTypedAsync<ArrayInput, ArrayInput>(_objId, _methId, input);

            // Assert
            Assert.NotNull(result.Numbers);
            Assert.Equal(2, result.Numbers.Length);
            Assert.Equal(10, result.Numbers[0]);
            Assert.Equal(20, result.Numbers[1]);
        }

        [Fact]
        public async Task CallTypedAsync_Throws_OnOutputCountMismatch()
        {
            // Arrange
            var input = new SimpleInput();
            // Server returns 2 variants, but SimpleOutput only expects 1
            _channelMock.Setup(c => c.CallAsync(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<CancellationToken>(), It.IsAny<Variant[]>()))
                .ReturnsAsync([
                    new Variant(true, BuiltInType.Boolean),
                    new Variant(1, BuiltInType.Int32)
                ]);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _channelMock.Object.CallTypedAsync<SimpleInput, SimpleOutput>(_objId, _methId, input));

            Assert.Equal("Output count mismatch", ex.Message);
        }

        [Fact]
        public async Task CallTypedAsync_HandlesNullInputProperty()
        {
            // Arrange
            var input = new SimpleInput { Id = 1, Name = null };

            _channelMock.Setup(c => c.CallAsync(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<CancellationToken>(), It.IsAny<Variant[]>()))
                .ReturnsAsync([new Variant(true, BuiltInType.Boolean)])
                .Callback<NodeId, NodeId, CancellationToken, Variant[]>((o, m, ct, vars) =>
                {
                    // Index 1 corresponds to 'Name' (Order 1)
                    Assert.Null(vars[1].Value);
                    Assert.Equal(BuiltInType.String, vars[1].Type);
                });

            // Act
            await _channelMock.Object.CallTypedAsync<SimpleInput, SimpleOutput>(_objId, _methId, input);
        }

        [Fact]
        public async Task CallTypedAsync_UnpacksExtensionObjectsInArray_Output()
        {
            // Arrange
            var data1 = new ComplexData { Val = "PartA" };
            var data2 = new ComplexData { Val = "PartB" };

            var extArr = new[] {
                new ExtensionObject { DecodedValue = data1 },
                new ExtensionObject { DecodedValue = data2 }
            };

            var variantArr = new Variant(extArr, BuiltInType.ExtensionObject, isArray: true);

            _channelMock.Setup(c => c.CallAsync(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<CancellationToken>(), It.IsAny<Variant[]>()))
                .ReturnsAsync([variantArr]);

            // Act
            var result = await _channelMock.Object.CallTypedAsync<SimpleInput, ArrayComplexOutput>(_objId, _methId, new SimpleInput());

            // Assert
            Assert.NotNull(result.Items);
            Assert.Equal(2, result.Items.Length);
            Assert.Equal("PartA", result.Items[0].Val);
            Assert.Equal("PartB", result.Items[1].Val);
        }

        [Fact]
        public async Task CallTypedAsync_PerformsAutomaticTypeConversion()
        {
            // Arrange
            // Server returns a SByte (byte 1), but C# DTO has a 'bool' property
            // Convert.ChangeType should handle basic conversions for IConvertible
            var outputVariants = new[] { new Variant((sbyte)1, BuiltInType.SByte) };

            _channelMock.Setup(c => c.CallAsync(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<CancellationToken>(), It.IsAny<Variant[]>()))
                .ReturnsAsync(outputVariants);

            // Act
            var result = await _channelMock.Object.CallTypedAsync<SimpleInput, SimpleOutput>(_objId, _methId, new SimpleInput());

            // Assert
            Assert.True(result.Success);
        }
    }

    public class SimpleInput
    {
        [OpcMethodParameter(1, BuiltInType.String)]
        public string? Name { get; set; }

        [OpcMethodParameter(0, BuiltInType.Int32)]
        public int Id { get; set; }
    }

    public class SimpleOutput
    {
        [OpcMethodParameter(0, BuiltInType.Boolean)]
        public bool Success { get; set; }
    }

    public class ComplexData
    {
        public string? Val { get; set; }
    }

    public class ComplexInput
    {
        [OpcMethodParameter(0, BuiltInType.ExtensionObject)]
        public ComplexData? Data { get; set; }
    }

    public class ArrayInput
    {
        [OpcMethodParameter(0, BuiltInType.Int32)]
        public int[]? Numbers { get; set; }
    }

    public class ArrayComplexOutput
    {
        [OpcMethodParameter(0, BuiltInType.ExtensionObject)]
        public ComplexData[]? Items { get; set; }
    }
}
