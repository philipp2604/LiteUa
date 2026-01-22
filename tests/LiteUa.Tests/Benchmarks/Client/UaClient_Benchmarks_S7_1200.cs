using LiteUa.BuiltIn;
using LiteUa.Security;
using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace LiteUa.Tests.Benchmarks.Client
{
    [Trait("Category", "Benchmark")]
    [Trait("Category", "S7-1200")]
    public class UaClient_S7_1200_Benchmarks : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly X509Certificate2 _testCertificate;

        public const string ServerUrl = "opc.tcp://192.178.0.1:4840/";
        public const string TestUser = "user";
        public const string TestPassword = "Password1";
        public const string ApplicationName = "LiteUa S7-1200 Benchmarks";
        public const string ApplicationUri = "urn:LiteUa:benchmark:s7-1200";
        public const string ProductUri = "urn:LiteUa:LiteUaBenchmarkS7-1200";

        public UaClient_S7_1200_Benchmarks(ITestOutputHelper output)
        {
            _output = output;
            _testCertificate = CertificateFactory.CreateSelfSignedCertificate(ApplicationName, ApplicationUri);
        }

        [Theory]
        [InlineData(MessageSecurityMode.None, SecurityPolicyType.None, UserTokenType.Anonymous)]
        [InlineData(MessageSecurityMode.Sign, SecurityPolicyType.Basic256Sha256, UserTokenType.Anonymous)]
        [InlineData(MessageSecurityMode.SignAndEncrypt, SecurityPolicyType.Basic256Sha256, UserTokenType.Anonymous)]
        [InlineData(MessageSecurityMode.None, SecurityPolicyType.None, UserTokenType.Username)]
        [InlineData(MessageSecurityMode.Sign, SecurityPolicyType.Basic256Sha256, UserTokenType.Username)]
        [InlineData(MessageSecurityMode.SignAndEncrypt, SecurityPolicyType.Basic256Sha256, UserTokenType.Username)]
        public async Task Run_Comprehensive_Benchmark(MessageSecurityMode messageSecurity, SecurityPolicyType policy, UserTokenType userType)
        {
            _output.WriteLine($"--- BENCHMARK START: Mode={messageSecurity}, Policy={policy}, User={userType} ---");

            var sw = new Stopwatch();

            // 1. Benchmark Connection (Handshake)
            sw.Start();
            await using var client = CreateClient(messageSecurity, policy, userType);
            await client.ConnectAsync();
            sw.Stop();
            var connectTime = sw.ElapsedMilliseconds;
            _output.WriteLine($"[1] Connection & Session Establishment: {connectTime} ms");

            // 2. Benchmark Read (Small Payload - 5 nodes)
            var nodesToRead = new NodeId[] { new(4, 3), new(4, 8), new(4, 13), new(4, 18), new(4, 72) };
            sw.Restart();
            var readResults = await client.ReadNodesAsync(nodesToRead);
            sw.Stop();
            _output.WriteLine($"[2] Read (5 Scalar Nodes): {sw.ElapsedMilliseconds} ms");

            // 3. Benchmark Read (Large Payload - 1 Array)
            var arrayNode = new NodeId(4, 73);
            sw.Restart();
            await client.ReadNodesAsync([arrayNode]);
            sw.Stop();
            _output.WriteLine($"[3] Read (1 Array Node): {sw.ElapsedMilliseconds} ms");

            // 4. Benchmark Write (Roundtrip)
            var writeNode = new NodeId(4, 226);
            var writeVal = new DataValue { Value = new Variant((short)123, BuiltInType.Int16) };
            sw.Restart();
            await client.WriteNodesAsync([writeNode], [writeVal]);
            sw.Stop();
            _output.WriteLine($"[4] Write (1 Scalar Node): {sw.ElapsedMilliseconds} ms");

            // 5. Benchmark Browse
            sw.Restart();
            await client.BrowseNodesAsync([new(0, 2253)]);
            sw.Stop();
            _output.WriteLine($"[5] Browse (Server Root): {sw.ElapsedMilliseconds} ms");

            // 6. Benchmark Method Call
            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = new Variant((short)10, BuiltInType.Int16);
            var valB = new Variant((short)20, BuiltInType.Int16);
            sw.Restart();
            await client.CallMethodAsync(objectNode, methodNode, default, valA, valB);
            sw.Stop();
            _output.WriteLine($"[6] Method Call (Add): {sw.ElapsedMilliseconds} ms");

            _output.WriteLine($"--- BENCHMARK END ---\n");
        }

        private UaClient CreateClient(MessageSecurityMode mode, SecurityPolicyType policy, UserTokenType userType)
        {
            return UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = mode;
                    s.PolicyType = policy;
                    s.UserTokenType = userType;

                    if (userType == UserTokenType.Username)
                    {
                        s.Username = TestUser;
                        s.Password = TestPassword;
                        s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    }
                    else
                    {
                        s.UserTokenPolicyType = SecurityPolicyType.None;
                    }
                })
                .WithSession(s => { 
                    s.ApplicationName = ApplicationName;
                    s.ApplicationUri = ApplicationUri;
                    s.ProductUri = ProductUri;
                })
                .WithPool(p => { p.MaxSize = 1; })
                .Build();
        }

        public void Dispose()
        {
            _testCertificate?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
