using LiteUa.Stack.View;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class BrowsePathParserTests
    {
        [Fact]
        public void Parse_SimplePath_ReturnsCorrectElements()
        {
            // Arrange
            string path = "Objects/Folder1/SensorA";

            // Act
            var result = BrowsePathParser.Parse(path);

            // Assert
            Assert.NotNull(result.Elements);
            Assert.Equal(3, result.Elements.Length);
            Assert.Equal("Objects", (string)result.Elements[0]!.TargetName!.Name);
            Assert.Equal(0, result.Elements[0].TargetName!.NamespaceIndex);
            Assert.Equal("Folder1", (string)result.Elements[1].TargetName!.Name);
            Assert.Equal("SensorA", (string)result.Elements[2].TargetName!.Name);
        }

        [Fact]
        public void Parse_PathWithNamespaces_ExtractsIndexesCorrectly()
        {
            // Arrange
            string path = "Objects/2:MyDevice/3:Status";

            // Act
            var result = BrowsePathParser.Parse(path);

            // Assert
            Assert.Equal(3, result.Elements!.Length);

            // Segment 1: Default NS 0
            Assert.Equal(0, result.Elements[0].TargetName!.NamespaceIndex);
            Assert.Equal("Objects", result.Elements[0].TargetName!.Name);

            // Segment 2: NS 2
            Assert.Equal(2, result.Elements[1].TargetName!.NamespaceIndex);
            Assert.Equal("MyDevice", result.Elements[1].TargetName!.Name);

            // Segment 3: NS 3
            Assert.Equal(3, result.Elements[2].TargetName!.NamespaceIndex);
            Assert.Equal("Status", result.Elements[2].TargetName!.Name);
        }

        [Fact]
        public void Parse_LeadingAndTrailingSlashes_IgnoresEmptySegments()
        {
            // Arrange
            string path = "/Objects//MyNode/";

            // Act
            var result = BrowsePathParser.Parse(path);

            // Assert
            Assert.NotNull(result.Elements);
            Assert.Equal(2, result.Elements.Length);
            Assert.Equal("Objects", result.Elements[0].TargetName!.Name);
            Assert.Equal("MyNode", result.Elements[1].TargetName!.Name);
        }

        [Fact]
        public void Parse_SetsStandardOpcUaDefaultsForElements()
        {
            // Arrange
            string path = "Node";

            // Act
            var result = BrowsePathParser.Parse(path);
            var element = result.Elements![0];

            // Assert
            // ReferenceTypeId 33 is HierarchicalReferences
            Assert.Equal(33u, element.ReferenceTypeId.NumericIdentifier);
            Assert.False(element.IsInverse);
            Assert.True(element.IncludeSubtypes);
        }

        [Theory]
        [InlineData("abc:Node")]    // Not a numeric namespace
        [InlineData(":Node")]       // Missing namespace part
        [InlineData("1:2:Node")]    // Too many colons
        public void Parse_InvalidNamespaceFormat_DefaultsToNamespaceZero(string invalidSegment)
        {
            // Act
            var result = BrowsePathParser.Parse(invalidSegment);

            // Assert
            Assert.Single(result.Elements!);
            Assert.Equal(0, result.Elements[0].TargetName!.NamespaceIndex);
            Assert.Equal(invalidSegment, result.Elements[0].TargetName!.Name);
        }

        [Fact]
        public void Parse_EmptyString_ReturnsRelativePathWithNoElements()
        {
            // Act
            var result = BrowsePathParser.Parse("");

            // Assert
            Assert.NotNull(result.Elements);
            Assert.Empty(result.Elements);
        }
    }
}