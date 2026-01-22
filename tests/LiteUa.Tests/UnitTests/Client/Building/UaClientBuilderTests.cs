using LiteUa.Client;
using LiteUa.Client.Building;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Client.Building
{
    [Trait("Category", "Unit")]
    public class UaClientBuilderTests
    {
        [Fact]
        public void ForEndpoint_SetsUrlAndReturnsBuilder()
        {
            // Arrange
            var builder = new UaClientBuilder();
            string url = "opc.tcp://localhost:4840";

            // Act
            var result = builder.ForEndpoint(url);

            // Assert
            Assert.Same(builder, result); // Verify fluent return
        }

        [Fact]
        public void WithSecurity_ExecutesConfigurationAction()
        {
            // Arrange
            var builder = new UaClientBuilder();
            bool actionCalled = false;

            // Act
            builder.WithSecurity(security =>
            {
                actionCalled = true;
            });

            // Assert
            Assert.True(actionCalled);
        }

        [Fact]
        public void WithSession_ExecutesConfigurationAction()
        {
            // Arrange
            var builder = new UaClientBuilder();
            bool actionCalled = false;

            // Act
            builder.WithSession(session =>
            {
                actionCalled = true;
            });

            // Assert
            Assert.True(actionCalled);
        }

        [Fact]
        public void WithPool_ExecutesConfigurationAction()
        {
            // Arrange
            var builder = new UaClientBuilder();
            bool actionCalled = false;

            // Act
            builder.WithPool(pool =>
            {
                actionCalled = true;
            });

            // Assert
            Assert.True(actionCalled);
        }

        [Fact]
        public void Build_ThrowsInvalidOperationException_WhenUrlMissing()
        {
            // Arrange
            var builder = new UaClientBuilder();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Equal("Endpoint URL must be set.", ex.Message);
        }

        [Fact]
        public void Build_ReturnsUaClient_WhenValid()
        {
            // Arrange
            var builder = new UaClientBuilder()
                .ForEndpoint("opc.tcp://127.0.0.1:4840");

            // Act
            var client = builder.Build();

            // Assert
            Assert.NotNull(client);
            Assert.IsType<UaClient>(client);
        }

        [Fact]
        public void Builder_ChainsCorrectly()
        {
            // Arrange & Act
            var client = new UaClientBuilder()
                .ForEndpoint("opc.tcp://localhost:4840")
                .WithSecurity(s => { /* config */ })
                .WithSession(s => { /* config */ })
                .Build();

            // Assert
            Assert.NotNull(client);
        }
    }
}
