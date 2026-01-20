using LiteUa.Encoding;
using LiteUa.Stack.Session;
using LiteUa.Transport.Headers;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Stack.Session
{
    [Trait("Category", "Unit")]
    public class CloseSessionResponseTests
    {
        private readonly Mock<OpcUaBinaryReader> _readerMock;

        public CloseSessionResponseTests()
        {
            // Mocking the reader. Ensure all Read methods are virtual in your OpcUaBinaryReader.
            _readerMock = new Mock<OpcUaBinaryReader>(new MemoryStream());
        }

        [Fact]
        public void Static_NodeId_IsCorrect()
        {
            // CloseSessionResponse identifier is 476
            Assert.Equal(476u, CloseSessionResponse.NodeId.NumericIdentifier);
        }
    }
}
