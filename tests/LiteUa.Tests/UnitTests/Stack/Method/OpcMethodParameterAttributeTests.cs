using LiteUa.BuiltIn;
using LiteUa.Stack.Method;
using System.Reflection;

namespace LiteUa.Tests.UnitTests.Stack.Method
{
    [Trait("Category", "Unit")]
    public class OpcMethodParameterAttributeTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            int expectedOrder = 5;
            BuiltInType expectedType = BuiltInType.Int32;

            // Act
            var attr = new OpcMethodParameterAttribute(expectedOrder, expectedType);

            // Assert
            Assert.Equal(expectedOrder, attr.Order);
            Assert.Equal(expectedType, attr.Type);
        }

        [Fact]
        public void AttributeUsage_IsTargetingProperties()
        {
            // Act
            var type = typeof(OpcMethodParameterAttribute);
            var usage = type.GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(usage);
            Assert.Equal(AttributeTargets.Property, usage.ValidOn);
        }

        // Helper class
        private class TestDto
        {
            [OpcMethodParameter(1, BuiltInType.String)]
            public string? Name { get; set; }

            [OpcMethodParameter(0, BuiltInType.UInt32)]
            public uint Id { get; set; }
        }

        [Fact]
        public void Reflection_CanRetrieveAttributeFromProperty()
        {
            // Arrange
            var property = typeof(TestDto).GetProperty(nameof(TestDto.Name));

            // Act
            var attr = property?.GetCustomAttribute<OpcMethodParameterAttribute>();

            // Assert
            Assert.NotNull(attr);
            Assert.Equal(1, attr!.Order);
            Assert.Equal(BuiltInType.String, attr.Type);
        }

        [Fact]
        public void Reflection_VerifyOrderingLogic()
        {
            // Arrange
            var properties = typeof(TestDto).GetProperties();

            // Act
            var sorted = properties
                .Select(p => new { Prop = p, Attr = p.GetCustomAttribute<OpcMethodParameterAttribute>() })
                .Where(x => x.Attr != null)
                .OrderBy(x => x.Attr!.Order)
                .ToList();

            // Assert
            Assert.Equal("Id", sorted[0].Prop.Name);   // Order 0
            Assert.Equal("Name", sorted[1].Prop.Name); // Order 1
        }
    }
}