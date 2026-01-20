using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Security;
using LiteUa.Security.Policies;
using LiteUa.Stack.Attribute;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session;
using LiteUa.Stack.View;
using LiteUa.Transport;
using LiteUa.Transport.Headers;
using Moq;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LiteUa.Tests.UnitTests.Transport
{
    [Trait("Category", "Unit")]
    public class UaTcpClientChannelFullTests : IDisposable
    {
        private readonly Mock<ISecurityPolicyFactory> _policyFactoryMock;
        private readonly Mock<ISecurityPolicy> _securityPolicyMock;
        private readonly string _endpointUrl = "opc.tcp://localhost:4840";
        private readonly X509Certificate2 _testCert;

        public UaTcpClientChannelFullTests()
        {
            _testCert = CreateTestCert();

            _policyFactoryMock = new Mock<ISecurityPolicyFactory>();
            _securityPolicyMock = new Mock<ISecurityPolicy>();
            _securityPolicyMock.Setup(p => p.SecurityPolicyUri).Returns("http://opcfoundation.org/UA/SecurityPolicy#None");
            _securityPolicyMock.Setup(p => p.AsymmetricSignatureSize).Returns(0);
            _securityPolicyMock.Setup(p => p.AsymmetricEncryptionBlockSize).Returns(1);
            _securityPolicyMock.Setup(p => p.AsymmetricCipherTextBlockSize).Returns(1);
            _securityPolicyMock.Setup(p => p.SymmetricSignatureSize).Returns(0);
            _securityPolicyMock.Setup(p => p.SymmetricBlockSize).Returns(1);
            _securityPolicyMock.Setup(p => p.EncryptAsymmetric(It.IsAny<byte[]>())).Returns<byte[]>(data => data);
            _securityPolicyMock.Setup(p => p.DecryptAsymmetric(It.IsAny<byte[]>())).Returns<byte[]>(data => data);
            _securityPolicyMock.Setup(p => p.Sign(It.IsAny<byte[]>())).Returns([]);
            _policyFactoryMock.Setup(f => f.CreateSecurityPolicy(It.IsAny<X509Certificate2>(), It.IsAny<X509Certificate2>()))
                .Returns(_securityPolicyMock.Object);
        }

        private TestableUaTcpClientChannel CreateSut()
        {
            return new TestableUaTcpClientChannel(
                _endpointUrl, "urn:test:app", "urn:test:prod", "name",
                _policyFactoryMock.Object, MessageSecurityMode.None, null, null);
        }

        #region Connection & Handshake

        [Fact]
        public async Task Handshake_Success_NegotiatesParameters()
        {
            var sut = CreateSut();

            // 1. HEL/ACK Handshake
            var ack = CreateAckPacket(8192, 4096);
            sut.EnqueueResponse(ack);

            // 2. OPN (OpenSecureChannel) response
            var opnBody = CreateOpenSecureChannelResponseBytes(1, 100);
            sut.EnqueueResponse(WrapInMessage("OPN", opnBody));

            await sut.ConnectAsync();

            Assert.Equal(8192u, sut.ReceiveBufferSize);
            Assert.Equal(4096u, sut.SendBufferSize);
            Assert.Equal(1u, sut.SecureChannelId);
        }

        [Fact]
        public async Task Handshake_ServerError_ThrowsException()
        {
            var sut = CreateSut();
            var err = CreateErrorPacket(0x80010000, "Bad Protocol");
            sut.EnqueueResponse(err);

            var ex = await Assert.ThrowsAsync<Exception>(() => sut.ConnectAsync());
            Assert.Contains("Bad Protocol", ex.Message);
        }

        #endregion

        #region Data Access (Read / Write)

        [Fact]
        public async Task ReadAsync_ReturnsDataValues()
        {
            var sut = CreateSut();
            SetupConnectedState(sut);

            var body = CreateResponseBody(634, w => {
                w.WriteInt32(1); // Results count
                w.WriteByte(0x01); // Mask: Value present
                w.WriteByte(0x06); // Type: Int32
                w.WriteInt32(1234);
            });
            sut.EnqueueResponse(WrapInMessage("MSG", body));

            var result = await sut.ReadAsync([new NodeId(1)]);

            Assert.NotNull(result);
            Assert.Equal(1234, result[0].Value?.Value);
        }

        [Fact]
        public async Task WriteAsync_ReturnsStatusCodes()
        {
            var sut = CreateSut();
            SetupConnectedState(sut);

            var body = CreateResponseBody(676, w => {
                w.WriteInt32(1); // Results count
                w.WriteUInt32(0); // Good
            });
            sut.EnqueueResponse(WrapInMessage("MSG", body));

            var result = await sut.WriteAsync([new NodeId(1)], [new DataValue()]);

            Assert.NotNull(result);
            Assert.True(result[0].IsGood);
        }

        #endregion

        #region View & Paging (Browse / Resolve)

        [Fact]
        public async Task BrowseAsync_MergesPagedResults()
        {
            var sut = CreateSut();
            SetupConnectedState(sut);

            // First Response with Continuation Point
            var body1 = CreateResponseBody(530, w => {
                w.WriteInt32(1); // Results count
                w.WriteUInt32(0); // StatusCode Good
                w.WriteByteString([0xEE]); // Continuation Point
                w.WriteInt32(1); // Ref count
                new NodeId(0, 1u).Encode(w); // RefTypeId
                w.WriteBoolean(true); // IsForward
                new ExpandedNodeId { NodeId = new NodeId(10) }.Encode(w); // NodeId
                new QualifiedName(0, "Node1").Encode(w); // BrowseName
                new LocalizedText { Text = "Node1" }.Encode(w); // DisplayName
                w.WriteUInt32(1); // NodeClass
                new ExpandedNodeId { NodeId = new NodeId(0) }.Encode(w); // TypeDef
            });

            // Second Response (BrowseNext)
            var body2 = CreateResponseBody(536, w => {
                w.WriteInt32(1); // Results count
                w.WriteUInt32(0); // Good
                w.WriteByteString(null); // No more points
                w.WriteInt32(1); // One more ref
                new NodeId(0, 1u).Encode(w);
                w.WriteBoolean(true);
                new ExpandedNodeId { NodeId = new NodeId(11) }.Encode(w);
                new QualifiedName(0, "Node2").Encode(w);
                new LocalizedText { Text = "Node2" }.Encode(w);
                w.WriteUInt32(1);
                new ExpandedNodeId { NodeId = new NodeId(0) }.Encode(w);
            });

            sut.EnqueueResponse(WrapInMessage("MSG", body1));
            sut.EnqueueResponse(WrapInMessage("MSG", body2));

            var results = await sut.BrowseAsync([new NodeId(0)]);

            Assert.Equal(2, results[0].Length);
            Assert.Equal("Node1", results[0][0].BrowseName?.Name);
            Assert.Equal("Node2", results[0][1].BrowseName?.Name);
        }

        [Fact]
        public async Task ResolveNodeIdsAsync_FiltersGoodResults()
        {
            var sut = CreateSut();
            SetupConnectedState(sut);

            var body = CreateResponseBody(557, w => {
                w.WriteInt32(2); // Two paths requested
                // Result 1: Success
                w.WriteUInt32(0); w.WriteInt32(1);
                new ExpandedNodeId { NodeId = new NodeId(500) }.Encode(w); w.WriteUInt32(0);
                // Result 2: Fail
                w.WriteUInt32(0x80010000); w.WriteInt32(0);
            });
            sut.EnqueueResponse(WrapInMessage("MSG", body));

            var result = await sut.ResolveNodeIdsAsync(new NodeId(0), ["P1", "P2"]);

            Assert.Equal(500u, result[0]!.NumericIdentifier);
            Assert.Null(result[1]);
        }

        #endregion

        #region Method Calls

        [Fact]
        public async Task CallAsync_ReturnsOutputArguments()
        {
            var sut = CreateSut();
            SetupConnectedState(sut);

            var body = CreateResponseBody(715, w => {
                w.WriteInt32(1); // Results
                w.WriteUInt32(0); // Call Success
                w.WriteInt32(0); // InputArg results
                w.WriteInt32(0); // InputArg diags
                w.WriteInt32(1); // OutputArgs count
                new Variant("ResultValue", BuiltInType.String).Encode(w);
            });
            sut.EnqueueResponse(WrapInMessage("MSG", body));

            var outputs = await sut.CallAsync(new NodeId(0), new NodeId(0));
            Assert.Equal("ResultValue", outputs[0].Value);
        }

        [Fact]
        public async Task CallAsync_Throws_OnServiceFault()
        {
            var sut = CreateSut();
            SetupConnectedState(sut);

            var body = CreateResponseBody(397, w => {
                WriteEmptyResponseHeader(w, 0x80010000); // ServiceFault
            });

            sut.EnqueueResponse(WrapInMessage("MSG", body));

            var ex = await Assert.ThrowsAsync<Exception>(() => sut.CallAsync(new NodeId(0), new NodeId(0)));
            Assert.Contains("ServiceFault", ex.Message);
        }

        #endregion

        #region Security & Validation

        [Fact]
        public void ValidateServerCertificate_FiresEvent()
        {
            var sut = CreateSut();
            bool eventFired = false;
            sut.CertificateValidation += (s, e) => { eventFired = true; e.Accept = true; };

            var method = typeof(UaTcpClientChannel).GetMethod("ValidateServerCertificate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            try { method!.Invoke(sut, [_testCert.RawData, true]); } catch { }

            Assert.True(eventFired);
        }

        #endregion

        #region Helper Factories

        private static byte[] CreateAckPacket(uint recv, uint send)
        {
            var p = new byte[28];
            System.Text.Encoding.ASCII.GetBytes("ACKF").CopyTo(p, 0);
            BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(4), 28);
            BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(12), recv);
            BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(16), send);
            return p;
        }

        private static byte[] CreateErrorPacket(uint code, string reason)
        {
            using var ms = new MemoryStream();
            var w = new OpcUaBinaryWriter(ms);
            w.WriteBytes(System.Text.Encoding.ASCII.GetBytes("ERRF"));
            w.WriteUInt32(0); // Placeholder
            w.WriteUInt32(code);
            w.WriteString(reason);
            var b = ms.ToArray();
            BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(4), (uint)b.Length);
            return b;
        }

        private static byte[] CreateResponseBody(uint nodeId, Action<OpcUaBinaryWriter> payloadWriter)
        {
            using var ms = new MemoryStream();
            var w = new OpcUaBinaryWriter(ms);
            w.WriteUInt32(1); // TokenId
            w.WriteUInt32(0); // Sequence
            w.WriteUInt32(0); // RequestId
            new NodeId(nodeId).Encode(w);
            if (nodeId != 397) WriteEmptyResponseHeader(w);
            payloadWriter(w);
            return ms.ToArray();
        }

        private static void WriteEmptyResponseHeader(OpcUaBinaryWriter w, uint result = 0)
        {
            w.WriteDateTime(DateTime.MinValue);
            w.WriteUInt32(0); // Handle
            w.WriteUInt32(result); // Result
            w.WriteByte(0);   // Diag Mask
            w.WriteInt32(0);  // StringTable
            new NodeId(0).Encode(w); // AdditionalHeader NodeId
            w.WriteByte(0);   // AdditionalHeader Mask
        }

        private static byte[] CreateOpenSecureChannelResponseBytes(uint channelId, uint tokenId)
        {
            using var ms = new MemoryStream();
            var w = new OpcUaBinaryWriter(ms);

            // 1. Mandatory Asymmetric Security Header
            w.WriteString("http://opcfoundation.org/UA/SecurityPolicy#None");
            w.WriteByteString(null);
            w.WriteByteString(null);

            // 2. Sequence Header
            w.WriteUInt32(1); w.WriteUInt32(1);

            // 3. Response payload
            new NodeId(449).Encode(w);
            WriteEmptyResponseHeader(w);
            w.WriteUInt32(0); // Version
            w.WriteUInt32(channelId);
            w.WriteUInt32(tokenId);
            w.WriteDateTime(DateTime.UtcNow);
            w.WriteUInt32(3600000); // Lifetime
            w.WriteByteString(new byte[32]); // ServerNonce
            return ms.ToArray();
        }

        private static void SetupConnectedState(TestableUaTcpClientChannel sut)
        {
            SetPrivateField(sut, "_stream", sut.DuplexStream);
            sut.SecureChannelId = 1;
            SetPrivateField(sut, "_tokenId", 1u);
        }

        private static byte[] WrapInMessage(string type, byte[] body)
        {
            var header = new byte[12 + body.Length];
            System.Text.Encoding.ASCII.GetBytes(type).CopyTo(header, 0);
            header[3] = (byte)'F';
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), (uint)header.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(8), 1);
            body.CopyTo(header, 12);
            return header;
        }

        private static X509Certificate2 CreateTestCert()
        {
            using RSA rsa = RSA.Create(2048);
            var req = new CertificateRequest($"CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
            return X509Certificate2.CreateFromPem(cert.ExportCertificatePem(), rsa.ExportPkcs8PrivateKeyPem());
        }

        private static void SetPrivateField(object obj, string name, object val)
        {
            var field = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?? obj.GetType().BaseType!.GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, val);
        }

        public void Dispose()
        {
            _testCert.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    internal class DuplexMemoryStream : Stream
    {
        private readonly Queue<byte[]> _responses = new();
        private readonly MemoryStream _incoming = new();
        private readonly MemoryStream _outgoing = new();
        public bool IsDisposed { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _incoming.Length;
        public override long Position { get => _incoming.Position; set => _incoming.Position = value; }

        public void Enqueue(byte[] data) => _responses.Enqueue(data);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (_incoming.Position >= _incoming.Length && _responses.Count > 0)
            {
                _incoming.SetLength(0);
                byte[] next = _responses.Dequeue();
                _incoming.Write(next);
                _incoming.Position = 0;
            }
            return await _incoming.ReadAsync(buffer, ct);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) => await _outgoing.WriteAsync(buffer, ct);
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _incoming.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _incoming.Seek(offset, origin);
        public override void SetLength(long value) => _incoming.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _outgoing.Write(buffer, offset, count);
        protected override void Dispose(bool disposing) { IsDisposed = true; base.Dispose(disposing); }
    }

    internal class TestableUaTcpClientChannel(string u, string a, string p, string n, ISecurityPolicyFactory f, MessageSecurityMode m, X509Certificate2? c, X509Certificate2? s) : UaTcpClientChannel(u, a, p, n, f, m, c, s)
    {
        public DuplexMemoryStream DuplexStream { get; } = new DuplexMemoryStream();

        protected override Task<Stream> CreateStreamAsync(string h, int p, CancellationToken ct) => Task.FromResult<Stream>(DuplexStream);
        public void EnqueueResponse(byte[] data) => DuplexStream.Enqueue(data);
    }
}