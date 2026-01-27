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
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LiteUa.Tests.UnitTests.Transport
{
    [Trait("Category", "Unit")]
    public class UaTcpClientChannelTests : IDisposable
    {
        private readonly Mock<ISecurityPolicyFactory> _policyFactoryMock;
        private readonly Mock<ISecurityPolicy> _securityPolicyMock;

        public UaTcpClientChannelTests()
        {
            _policyFactoryMock = new Mock<ISecurityPolicyFactory>();
            _securityPolicyMock = new Mock<ISecurityPolicy>();

            _securityPolicyMock.Setup(p => p.SecurityPolicyUri).Returns("http://opcfoundation.org/UA/SecurityPolicy#None");
            _securityPolicyMock.Setup(p => p.AsymmetricEncryptionBlockSize).Returns(1);
            _securityPolicyMock.Setup(p => p.AsymmetricCipherTextBlockSize).Returns(1);
            _securityPolicyMock.Setup(p => p.AsymmetricSignatureSize).Returns(0);
            _securityPolicyMock.Setup(p => p.SymmetricBlockSize).Returns(1);
            _securityPolicyMock.Setup(p => p.SymmetricSignatureSize).Returns(0);
            _securityPolicyMock.Setup(p => p.DecryptAsymmetric(It.IsAny<byte[]>())).Returns<byte[]>(d => d);
            _securityPolicyMock.Setup(p => p.DecryptSymmetric(It.IsAny<byte[]>())).Returns<byte[]>(d => d);
            _securityPolicyMock.Setup(p => p.EncryptAsymmetric(It.IsAny<byte[]>())).Returns<byte[]>(d => d);
            _securityPolicyMock.Setup(p => p.EncryptSymmetric(It.IsAny<byte[]>())).Returns<byte[]>(d => d);
            _securityPolicyMock.Setup(p => p.Sign(It.IsAny<byte[]>())).Returns(Array.Empty<byte>());
            _policyFactoryMock.Setup(f => f.CreateSecurityPolicy(It.IsAny<X509Certificate2>(), It.IsAny<X509Certificate2>())).Returns(_securityPolicyMock.Object);
        }

        [Fact]
        public async Task FullCommunication_NoHanging_Test()
        {
            using var sut = new TestableUaTcpClientChannel("opc.tcp://localhost:4840", "app", "prod", "name", _policyFactoryMock.Object);

            var serverTask = Task.Run(async () =>
            {
                // 1. Handshake
                await sut.ServerStream.ReadPacketFromSutAsync(); // HELLO
                sut.ServerStream.EnqueueToSut(PacketFactory.CreateAck());

                // 2. OpenChannel
                var opnReq = await sut.ServerStream.ReadPacketFromSutAsync();
                uint opnId = PacketFactory.ExtractId(opnReq, true);
                sut.ServerStream.EnqueueToSut(PacketFactory.CreateOpenSecureChannelResponse(opnId));

                // 3. Read
                var readReq = await sut.ServerStream.ReadPacketFromSutAsync();
                uint readId = PacketFactory.ExtractId(readReq, false);
                sut.ServerStream.EnqueueToSut(PacketFactory.CreateServiceResponse(634, readId, w => {
                    w.WriteInt32(1); w.WriteByte(1); w.WriteByte(6); w.WriteInt32(42);
                }));
            });

            await sut.ConnectAsync();
            var res = await sut.ReadAsync([new NodeId(2000)]);

            Assert.Equal(42, res?[0].Value?.Value);
        }

        public void Dispose() => GC.SuppressFinalize(this);
    }

    internal class SimulatedSocketStream : Stream
    {
        private readonly BlockingCollection<byte[]> _toSutQueue = new();
        private readonly BlockingCollection<byte[]> _fromSutQueue = new();
        private byte[]? _toSutBuf; int _toSutPos;
        private byte[]? _fromSutBuf; int _fromSutPos;

        public void EnqueueToSut(byte[] data) => _toSutQueue.Add(data);

        // SUT calls this
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            while (_toSutBuf == null || _toSutPos >= _toSutBuf.Length)
            {
                if (!_toSutQueue.TryTake(out _toSutBuf, 5000)) throw new TimeoutException("SUT Read Timeout");
                _toSutPos = 0;
            }
            int len = Math.Min(buffer.Length, _toSutBuf.Length - _toSutPos);
            _toSutBuf.AsMemory(_toSutPos, len).CopyTo(buffer);
            _toSutPos += len;
            return await Task.FromResult(len);
        }

        // SUT calls this
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        {
            _fromSutQueue.Add(buffer.ToArray());
            return ValueTask.CompletedTask;
        }

        // Mock Server calls this
        public async Task<byte[]> ReadPacketFromSutAsync()
        {
            var header = await InternalReadFromSut(8);
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));
            var body = await InternalReadFromSut((int)size - 8);
            return [.. header, .. body];
        }

        private async Task<byte[]> InternalReadFromSut(int count)
        {
            byte[] res = new byte[count];
            int filled = 0;
            while (filled < count)
            {
                while (_fromSutBuf == null || _fromSutPos >= _fromSutBuf.Length)
                {
                    if (!_fromSutQueue.TryTake(out _fromSutBuf, 5000)) throw new TimeoutException("Mock Server Read Timeout");
                    _fromSutPos = 0;
                }
                int available = Math.Min(count - filled, _fromSutBuf.Length - _fromSutPos);
                Buffer.BlockCopy(_fromSutBuf, _fromSutPos, res, filled, available);
                _fromSutPos += available;
                filled += available;
            }
            return await Task.FromResult(res);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override long Length => 0;
        public override long Position { get; set; }
        public override void Flush() { }
        public override int Read(byte[] b, int o, int c) => ReadAsync(b.AsMemory(o, c)).GetAwaiter().GetResult();
        public override void Write(byte[] b, int o, int c) => WriteAsync(b.AsMemory(o, c)).GetAwaiter().GetResult();
        public override long Seek(long o, SeekOrigin r) => 0;
        public override void SetLength(long v) { }
    }

    internal class TestableUaTcpClientChannel(string u, string a, string p, string n, ISecurityPolicyFactory f)
        : UaTcpClientChannel(u, a, p, n, f, MessageSecurityMode.None, null, null)
    {
        public SimulatedSocketStream ServerStream { get; } = new();
        protected override Task<Stream> CreateStreamAsync(string h, int p, CancellationToken ct) => Task.FromResult<Stream>(ServerStream);
    }

    internal static class PacketFactory
    {
        public static uint ExtractId(byte[] packet, bool isOpn)
        {
            using var ms = new MemoryStream(packet, 12, packet.Length - 12);
            var r = new OpcUaBinaryReader(ms);
            if (isOpn) { r.ReadString(); r.ReadByteString(); r.ReadByteString(); }
            else { r.ReadUInt32(); }
            r.ReadUInt32(); return r.ReadUInt32();
        }

        public static byte[] CreateAck()
        {
            var b = new byte[28];
            Array.Copy("ACKF"u8.ToArray(), b, 4);
            BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(4), 28);
            return b;
        }

        public static byte[] CreateOpenSecureChannelResponse(uint id)
        {
            using var ms = new MemoryStream();
            var w = new OpcUaBinaryWriter(ms);
            w.WriteUInt32(1); w.WriteString("http://opcfoundation.org/UA/SecurityPolicy#None");
            w.WriteByteString(null); w.WriteByteString(null);
            w.WriteUInt32(1); w.WriteUInt32(id);
            new NodeId(449).Encode(w);
            w.WriteDateTime(DateTime.UtcNow); w.WriteUInt32(0); w.WriteUInt32(0);
            w.WriteByte(0); w.WriteInt32(0); new NodeId(0).Encode(w); w.WriteByte(0);
            w.WriteUInt32(0); w.WriteUInt32(1); w.WriteUInt32(100);
            w.WriteDateTime(DateTime.UtcNow); w.WriteUInt32(3600000); w.WriteByteString(new byte[32]);
            var body = ms.ToArray();
            var head = new byte[8];
            Array.Copy("OPNF"u8.ToArray(), head, 4);
            BinaryPrimitives.WriteUInt32LittleEndian(head.AsSpan(4), (uint)body.Length + 8);
            return [.. head, .. body];
        }

        public static byte[] CreateServiceResponse(uint type, uint id, Action<OpcUaBinaryWriter> action)
        {
            using var ms = new MemoryStream();
            var w = new OpcUaBinaryWriter(ms);
            w.WriteUInt32(1); w.WriteUInt32(id);
            new NodeId(type).Encode(w);
            w.WriteDateTime(DateTime.UtcNow); w.WriteUInt32(0); w.WriteUInt32(0);
            w.WriteByte(0); w.WriteInt32(0); new NodeId(0).Encode(w); w.WriteByte(0);
            action(w);
            var body = ms.ToArray();
            var head = new byte[16];
            Array.Copy("MSGF"u8.ToArray(), head, 4);
            BinaryPrimitives.WriteUInt32LittleEndian(head.AsSpan(4), (uint)body.Length + 16);
            BinaryPrimitives.WriteUInt32LittleEndian(head.AsSpan(8), 1);
            BinaryPrimitives.WriteUInt32LittleEndian(head.AsSpan(12), 100);
            return [.. head, .. body];
        }
    }
}