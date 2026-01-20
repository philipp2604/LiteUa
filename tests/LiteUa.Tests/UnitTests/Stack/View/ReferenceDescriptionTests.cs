using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.View;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.View
{
    [Trait("Category", "Unit")]
    public class ReferenceDescriptionTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public ReferenceDescriptionTests()
        {
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Decode_ValidStream_SetsAllProperties()
        {
            // Arrange
            _readerMock.SetupSequence(r => r.ReadByte())
                .Returns(0x00) // NodeId: TwoByte
                .Returns(33)   // NodeId: ID (Hierarchical)
                .Returns(0x00) // NodeId (Expanded): TwoByte
                .Returns(101)  // NodeId (Expanded): ID
                .Returns(0x00) // LocalizedText: Mask 0
                .Returns(0x00); // TypeDefinition: TwoByte
            _readerMock.Setup(r => r.ReadBoolean()).Returns(true); // IsForward
            _readerMock.Setup(r => r.ReadUInt16()).Returns(1);
            _readerMock.Setup(r => r.ReadString()).Returns("MyBrowseName");
            _readerMock.Setup(r => r.ReadUInt32()).Returns(2u); // NodeClass 2 (Variable)

            // Act
            var result = ReferenceDescription.Decode(_readerMock.Object);

            // Assert
            Assert.NotNull(result.ReferenceTypeId);
            Assert.Equal(33u, result.ReferenceTypeId.NumericIdentifier);
            Assert.True(result.IsForward);
            Assert.NotNull(result.NodeId);
            Assert.Equal(101u, result.NodeId.NodeId.NumericIdentifier);
            Assert.Equal("MyBrowseName", (string)result.BrowseName!.Name);
            Assert.Equal(2u, result.NodeClass);
        }

        [Fact]
        public void Decode_VerifiesStrictFieldOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _readerMock.Setup(r => r.ReadBoolean()).Returns(() => {
                callOrder.Add("Boolean");
                return true;
            });

            _readerMock.Setup(r => r.ReadUInt32()).Returns(() => {
                callOrder.Add("UInt32");
                return 0u;
            });

            _readerMock.Setup(r => r.ReadString()).Returns(() => {
                callOrder.Add("String");
                return "ValidName";
            });

            _readerMock.Setup(r => r.ReadByte()).Returns(0);
            _readerMock.Setup(r => r.ReadUInt16()).Returns(0);

            // Act
            ReferenceDescription.Decode(_readerMock.Object);

            // Assert
            // ReferenceTypeId (NodeId) -> IsForward (Bool) -> NodeId (ExpandedNodeId) -> 
            // BrowseName (QualifiedName) -> DisplayName (LocalizedText) -> NodeClass (UInt32) -> TypeDefinition (ExpandedNodeId)

            int boolIdx = callOrder.IndexOf("Boolean");
            int stringIdx = callOrder.IndexOf("String");
            int uintIdx = callOrder.IndexOf("UInt32");

            Assert.True(boolIdx < stringIdx);
            Assert.True(stringIdx < uintIdx);
        }

        [Fact]
        public void Properties_AreMutable()
        {
            // Arrange
            var desc = new ReferenceDescription();
            var id = new NodeId(10);

            // Act
            desc.ReferenceTypeId = id;
            desc.NodeClass = 1;

            // Assert
            Assert.Same(id, desc.ReferenceTypeId);
            Assert.Equal(1u, desc.NodeClass);
        }
    }
}
