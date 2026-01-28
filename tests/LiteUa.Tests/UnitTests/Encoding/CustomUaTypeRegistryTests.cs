using LiteUa.BuiltIn;
using LiteUa.Encoding;
using Moq;

namespace LiteUa.Tests.UnitTests.Encoding
{
    [Trait("Category", "Unit")]
    public class CustomUaTypeRegistryTests : IDisposable
    {
        // Dummy class
        private class MyCustomType
        {
            public int Id { get; set; }
        }

        public CustomUaTypeRegistryTests()
        {
            // Ensure a clean state before every test
            CustomUaTypeRegistry.Clear();
        }

        public void Dispose()
        {
            // Cleanup after every test
            CustomUaTypeRegistry.Clear();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void Register_And_TryGet_ReturnsCorrectDelegates()
        {
            // Arrange
            var encodingId = new NodeId(1, 500u);
            static MyCustomType decoder(OpcUaBinaryReader r) => new() { Id = r.ReadInt32() };
            static void encoder(MyCustomType obj, OpcUaBinaryWriter w) => w.WriteInt32(obj.Id);

            // Act
            CustomUaTypeRegistry.Register(encodingId, decoder, encoder);

            // Assert Decoder
            bool decoderFound = CustomUaTypeRegistry.TryGetDecoder(encodingId, out var retrievedDecoder);
            Assert.True(decoderFound);
            Assert.NotNull(retrievedDecoder);

            // Assert Encoder
            bool encoderFound = CustomUaTypeRegistry.TryGetEncoder(typeof(MyCustomType), out var retrievedEncoder, out var retrievedId);
            Assert.True(encoderFound);
            Assert.NotNull(retrievedEncoder);
            Assert.Equal(encodingId, retrievedId);
        }

        [Fact]
        public void TryGetDecoder_NonExistentId_ReturnsFalse()
        {
            // Arrange
            var unknownId = new NodeId(1, 999u);

            // Act
            bool found = CustomUaTypeRegistry.TryGetDecoder(unknownId, out var decoder);

            // Assert
            Assert.False(found);
            Assert.Null(decoder);
        }

        [Fact]
        public void TryGetEncoder_NonExistentType_ReturnsFalse()
        {
            // Act
            bool found = CustomUaTypeRegistry.TryGetEncoder(typeof(string), out var encoder, out var id);

            // Assert
            Assert.False(found);
            Assert.Null(encoder);
            Assert.Null(id);
        }

        [Fact]
        public void Unregister_RemovesTypeAndIdReferences()
        {
            // Arrange
            var encodingId = new NodeId(1, 100u);
            CustomUaTypeRegistry.Register<MyCustomType>(encodingId, r => new(), (o, w) => { });

            // Act
            CustomUaTypeRegistry.Unregister<MyCustomType>();

            // Assert
            Assert.False(CustomUaTypeRegistry.TryGetDecoder(encodingId, out _));
            Assert.False(CustomUaTypeRegistry.TryGetEncoder(typeof(MyCustomType), out _, out _));
        }

        [Fact]
        public void Clear_EmptyAllDictionaries()
        {
            // Arrange
            CustomUaTypeRegistry.Register<MyCustomType>(new NodeId(1), r => new(), (o, w) => { });
            CustomUaTypeRegistry.Register<int>(new NodeId(2), r => 0, (o, w) => { });

            // Act
            CustomUaTypeRegistry.Clear();

            // Assert
            Assert.False(CustomUaTypeRegistry.TryGetDecoder(new NodeId(1), out _));
            Assert.False(CustomUaTypeRegistry.TryGetEncoder(typeof(int), out _, out _));
        }

        [Fact]
        public void Register_DuplicateId_IgnoredByLogic()
        {
            // Arrange
            var id = new NodeId(1, 100u);
            CustomUaTypeRegistry.Register<MyCustomType>(id, r => new MyCustomType { Id = 1 }, (o, w) => { });

            // Try to register a different logic with the same NodeId
            CustomUaTypeRegistry.Register<MyCustomType>(id, r => new MyCustomType { Id = 2 }, (o, w) => { });

            // Act
            CustomUaTypeRegistry.TryGetDecoder(id, out var decoder);
            var mockReader = new Mock<OpcUaBinaryReader>(new MemoryStream());
            var result = (MyCustomType)decoder!(mockReader.Object);

            // Assert
            Assert.Equal(1, result.Id); // The first registration was preserved
        }

        [Fact]
        public async Task ThreadSafety_LockIsUsed()
        {
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                int localI = i;
                tasks.Add(Task.Run(() =>
                {
                    CustomUaTypeRegistry.Register(new NodeId((ushort)localI), r => localI, (o, w) => { });
                }));
            }

            await Task.WhenAll([.. tasks]);
        }
    }
}