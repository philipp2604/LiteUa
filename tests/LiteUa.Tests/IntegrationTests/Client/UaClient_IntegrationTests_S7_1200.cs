using LiteUa.BuiltIn;
using LiteUa.Client;
using LiteUa.Client.Subscriptions;
using LiteUa.Encoding;
using LiteUa.Security;
using LiteUa.Security.Policies;
using LiteUa.Stack.Method;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit.Abstractions;

namespace LiteUa.Tests.IntegrationTests.Client
{
    [Trait("Category", "Integration")]
    [Trait("Category", "S7-1200")]
    public class UaClient_IntegrationTests_S7_1200
    {
        private readonly ITestOutputHelper _output;
        private readonly X509Certificate2 _testCertificate;
        public const string ServerUrl = "opc.tcp://192.178.0.1:4840/";
        public const string TestUser = "user";
        public const string TestPassword = "Password1";

        public UaClient_IntegrationTests_S7_1200(ITestOutputHelper output)
        {
            _output = output;
            _testCertificate = CertificateFactory.CreateSelfSignedCertificate("LiteUa Client", "urn:LiteUa:client");
            RegisterCustomTypes();
        }

        private static void RegisterCustomTypes()
        {
            CustomUaTypeRegistry.Register<TestStruct>(new NodeId(4, 258), TestStruct.Decode, (val, w) => val.Encode(w));
        }

        #region Connect, Subscribe and Reconnect tests

        [Fact]
        public async Task Connect_And_Subscribe_Should_Reconnect_On_Loss_Anonymous_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.None;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcsInitial = new TaskCompletionSource<bool>();
            var tcsAfterReconnect = new TaskCompletionSource<bool>();
            int updateCount = 0;

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254)
                ],
                [
                    (h, v) =>
                    {
                        updateCount++;

                        if(!tcsInitial.Task.IsCompleted)
                            tcsInitial.TrySetResult(true);
                        else
                            tcsAfterReconnect.TrySetResult(true);

                    }
                ]);

            var result1 = await tcsInitial.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.True(result1);

            var subClientField = typeof(UaClient).GetField("_subscriptionClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var subClient = (SubscriptionClient?)subClientField?.GetValue(client);
            var channelField = typeof(SubscriptionClient).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (UaTcpClientChannel?)channelField?.GetValue(subClient);
            if (channel != null)
                await channel.DisposeAsync();

            await Task.Delay(8000);

            var result2 = await tcsAfterReconnect.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task Connect_And_Subscribe_Should_Reconnect_On_Loss_Anonymous_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcsInitial = new TaskCompletionSource<bool>();
            var tcsAfterReconnect = new TaskCompletionSource<bool>();
            int updateCount = 0;

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254)
                ],
                [
                    (h, v) =>
                    {
                        updateCount++;

                        if(!tcsInitial.Task.IsCompleted)
                            tcsInitial.TrySetResult(true);
                        else
                            tcsAfterReconnect.TrySetResult(true);

                    }
                ]);

            var result1 = await tcsInitial.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.True(result1);

            var subClientField = typeof(UaClient).GetField("_subscriptionClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var subClient = (SubscriptionClient?)subClientField?.GetValue(client);
            var channelField = typeof(SubscriptionClient).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (UaTcpClientChannel?)channelField?.GetValue(subClient);
            if (channel != null)
                await channel.DisposeAsync();

            await Task.Delay(8000);

            var result2 = await tcsAfterReconnect.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task Connect_And_Subscribe_Should_Reconnect_On_Loss_Anonymous_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcsInitial = new TaskCompletionSource<bool>();
            var tcsAfterReconnect = new TaskCompletionSource<bool>();
            int updateCount = 0;

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254)
                ],
                [
                    (h, v) =>
                    {
                        updateCount++;

                        if(!tcsInitial.Task.IsCompleted)
                            tcsInitial.TrySetResult(true);
                        else
                            tcsAfterReconnect.TrySetResult(true);

                    }
                ]);

            var result1 = await tcsInitial.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.True(result1);

            var subClientField = typeof(UaClient).GetField("_subscriptionClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var subClient = (SubscriptionClient?)subClientField?.GetValue(client);
            var channelField = typeof(SubscriptionClient).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (UaTcpClientChannel?)channelField?.GetValue(subClient);
            if (channel != null)
                await channel.DisposeAsync();

            await Task.Delay(8000);

            var result2 = await tcsAfterReconnect.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task Connect_And_Subscribe_Should_Reconnect_On_Loss_Username_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcsInitial = new TaskCompletionSource<bool>();
            var tcsAfterReconnect = new TaskCompletionSource<bool>();
            int updateCount = 0;

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254)
                ],
                [
                    (h, v) =>
                    {
                        updateCount++;

                        if(!tcsInitial.Task.IsCompleted)
                            tcsInitial.TrySetResult(true);
                        else
                            tcsAfterReconnect.TrySetResult(true);

                    }
                ]);

            var result1 = await tcsInitial.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.True(result1);

            var subClientField = typeof(UaClient).GetField("_subscriptionClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var subClient = (SubscriptionClient?)subClientField?.GetValue(client);
            var channelField = typeof(SubscriptionClient).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (UaTcpClientChannel?)channelField?.GetValue(subClient);
            if (channel != null)
                await channel.DisposeAsync();

            await Task.Delay(8000);

            var result2 = await tcsAfterReconnect.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task Connect_And_Subscribe_Should_Reconnect_On_Loss_Username_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcsInitial = new TaskCompletionSource<bool>();
            var tcsAfterReconnect = new TaskCompletionSource<bool>();
            int updateCount = 0;

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254)
                ],
                [
                    (h, v) =>
                    {
                        updateCount++;

                        if(!tcsInitial.Task.IsCompleted)
                            tcsInitial.TrySetResult(true);
                        else
                            tcsAfterReconnect.TrySetResult(true);

                    }
                ]);

            var result1 = await tcsInitial.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.True(result1);

            var subClientField = typeof(UaClient).GetField("_subscriptionClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var subClient = (SubscriptionClient?)subClientField?.GetValue(client);
            var channelField = typeof(SubscriptionClient).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (UaTcpClientChannel?)channelField?.GetValue(subClient);
            if (channel != null)
                await channel.DisposeAsync();

            await Task.Delay(8000);

            var result2 = await tcsAfterReconnect.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task Connect_And_Subscribe_Should_Reconnect_On_Loss_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcsInitial = new TaskCompletionSource<bool>();
            var tcsAfterReconnect = new TaskCompletionSource<bool>();
            int updateCount = 0;

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254)
                ],
                [
                    (h, v) =>
                    {
                        updateCount++;

                        if(!tcsInitial.Task.IsCompleted)
                            tcsInitial.TrySetResult(true);
                        else
                            tcsAfterReconnect.TrySetResult(true);

                    }
                ]);

            var result1 = await tcsInitial.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.True(result1);

            var subClientField = typeof(UaClient).GetField("_subscriptionClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var subClient = (SubscriptionClient?)subClientField?.GetValue(client);
            var channelField = typeof(SubscriptionClient).GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (UaTcpClientChannel?)channelField?.GetValue(subClient);
            if (channel != null)
                await channel.DisposeAsync();

            await Task.Delay(8000);

            var result2 = await tcsAfterReconnect.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        #endregion

        #region Connect and Subscribe tests

        [Fact]
        public async Task Connect_And_Subscribe_Anonymous_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.None;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254),
                    new(0, 2255)
                ],
                [
                    (h, v) => { tcs1.TrySetResult(v.StatusCode.IsGood); },
                    (h, v) => { tcs2.TrySetResult(v.StatusCode.IsGood); }
                ]);

            var result1 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var result2 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task Connect_And_Subscribe_Anonymous_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254),
                    new(0, 2255)
                ],
                [
                    (h, v) => { tcs1.TrySetResult(v.StatusCode.IsGood); },
                    (h, v) => { tcs2.TrySetResult(v.StatusCode.IsGood); }
                ]);

            var result1 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var result2 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task Connect_And_Subscribe_Anonymous_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254),
                    new(0, 2255)
                ],
                [
                    (h, v) => { tcs1.TrySetResult(v.StatusCode.IsGood); },
                    (h, v) => { tcs2.TrySetResult(v.StatusCode.IsGood); }
                ]);

            var result1 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var result2 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task Connect_And_Subscribe_Username_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254),
                    new(0, 2255)
                ],
                [
                    (h, v) => { tcs1.TrySetResult(v.StatusCode.IsGood); },
                    (h, v) => { tcs2.TrySetResult(v.StatusCode.IsGood); }
                ]);

            var result1 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var result2 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task Connect_And_Subscribe_Username_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254),
                    new(0, 2255)
                ],
                [
                    (h, v) => { tcs1.TrySetResult(v.StatusCode.IsGood); },
                    (h, v) => { tcs2.TrySetResult(v.StatusCode.IsGood); }
                ]);

            var result1 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var result2 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task Connect_And_Subscribe_Username_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();

            // 2. Act
            await client.ConnectAsync();
            await client.SubscribeAsync(
                [
                    new(0, 2254),
                    new(0, 2255)
                ],
                [
                    (h, v) => { tcs1.TrySetResult(v.StatusCode.IsGood); },
                    (h, v) => { tcs2.TrySetResult(v.StatusCode.IsGood); }
                ]);

            var result1 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var result2 = await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // 3. Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        #endregion Connect and Subscribe tests

        #region Connect and Read tests

        [Fact]
        public async Task Connect_And_Read_Anonymous_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.None;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodesToRead = new NodeId[_readVariables.Length];

            for (int i = 0; i < _readVariables.Length; i++)
            {
                nodesToRead[i] = _readVariables[i].nodeId;
            }

            // 2. Act
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodesToRead);

            // 3. Assert
            Assert.NotNull(readResults);
            Assert.Equal(_readVariables.Length, readResults.Length);

            for (int i = 0; i < readResults.Length; i++)
            {
                Assert.NotNull(readResults[i].Value);
                Assert.NotNull(readResults[i].Value!.Value);
                Assert.True(readResults[i].StatusCode.IsGood);

                Assert.Equal(_readVariables[i].type, readResults[i].Value!.Type);
                Assert.Equal(_readVariables[i].isArray, readResults[i].Value!.IsArray);

                if (_readVariables[i].isArray)
                {
                    Assert.Equal(3, ((Array)(readResults[i].Value!.Value)!).Length); // all test arrays are of length 3
                    return;
                }
            }
        }

        [Fact]
        public async Task Connect_And_Read_Anonymous_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodesToRead = new NodeId[_readVariables.Length];

            for (int i = 0; i < _readVariables.Length; i++)
            {
                nodesToRead[i] = _readVariables[i].nodeId;
            }

            // 2. Act
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodesToRead);

            // 3. Assert
            Assert.NotNull(readResults);
            Assert.Equal(_readVariables.Length, readResults.Length);

            for (int i = 0; i < readResults.Length; i++)
            {
                Assert.NotNull(readResults[i].Value);
                Assert.NotNull(readResults[i].Value!.Value);
                Assert.True(readResults[i].StatusCode.IsGood);

                Assert.Equal(_readVariables[i].type, readResults[i].Value!.Type);
                Assert.Equal(_readVariables[i].isArray, readResults[i].Value!.IsArray);

                if (_readVariables[i].isArray)
                {
                    Assert.Equal(3, ((Array)(readResults[i].Value!.Value)!).Length); // all test arrays are of length 3
                    return;
                }
            }
        }

        [Fact]
        public async Task Connect_And_Read_Anonymous_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodesToRead = new NodeId[_readVariables.Length];

            for (int i = 0; i < _readVariables.Length; i++)
            {
                nodesToRead[i] = _readVariables[i].nodeId;
            }

            // 2. Act
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodesToRead);

            // 3. Assert
            Assert.NotNull(readResults);
            Assert.Equal(_readVariables.Length, readResults.Length);

            for (int i = 0; i < readResults.Length; i++)
            {
                Assert.NotNull(readResults[i].Value);
                Assert.NotNull(readResults[i].Value!.Value);
                Assert.True(readResults[i].StatusCode.IsGood);

                Assert.Equal(_readVariables[i].type, readResults[i].Value!.Type);
                Assert.Equal(_readVariables[i].isArray, readResults[i].Value!.IsArray);

                if (_readVariables[i].isArray)
                {
                    Assert.Equal(3, ((Array)(readResults[i].Value!.Value)!).Length); // all test arrays are of length 3
                    return;
                }
            }
        }

        [Fact]
        public async Task Connect_And_Read_Username_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodesToRead = new NodeId[_readVariables.Length];

            for (int i = 0; i < _readVariables.Length; i++)
            {
                nodesToRead[i] = _readVariables[i].nodeId;
            }

            // 2. Act
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodesToRead);

            // 3. Assert
            Assert.NotNull(readResults);
            Assert.Equal(_readVariables.Length, readResults.Length);

            for (int i = 0; i < readResults.Length; i++)
            {
                Assert.NotNull(readResults[i].Value);
                Assert.NotNull(readResults[i].Value!.Value);
                Assert.True(readResults[i].StatusCode.IsGood);

                Assert.Equal(_readVariables[i].type, readResults[i].Value!.Type);
                Assert.Equal(_readVariables[i].isArray, readResults[i].Value!.IsArray);

                if (_readVariables[i].isArray)
                {
                    Assert.Equal(3, ((Array)(readResults[i].Value!.Value)!).Length); // all test arrays are of length 3
                    return;
                }
            }
        }

        [Fact]
        public async Task Connect_And_Read_Username_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodesToRead = new NodeId[_readVariables.Length];

            for (int i = 0; i < _readVariables.Length; i++)
            {
                nodesToRead[i] = _readVariables[i].nodeId;
            }

            // 2. Act
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodesToRead);

            // 3. Assert
            Assert.NotNull(readResults);
            Assert.Equal(_readVariables.Length, readResults.Length);

            for (int i = 0; i < readResults.Length; i++)
            {
                Assert.NotNull(readResults[i].Value);
                Assert.NotNull(readResults[i].Value!.Value);
                Assert.True(readResults[i].StatusCode.IsGood);

                Assert.Equal(_readVariables[i].type, readResults[i].Value!.Type);
                Assert.Equal(_readVariables[i].isArray, readResults[i].Value!.IsArray);

                if (_readVariables[i].isArray)
                {
                    Assert.Equal(3, ((Array)(readResults[i].Value!.Value)!).Length); // all test arrays are of length 3
                    return;
                }
            }
        }

        [Fact]
        public async Task Connect_And_Read_Username_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodesToRead = new NodeId[_readVariables.Length];

            for (int i = 0; i < _readVariables.Length; i++)
            {
                nodesToRead[i] = _readVariables[i].nodeId;
            }

            // 2. Act
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodesToRead);

            // 3. Assert
            Assert.NotNull(readResults);
            Assert.Equal(_readVariables.Length, readResults.Length);

            for (int i = 0; i < readResults.Length; i++)
            {
                Assert.NotNull(readResults[i].Value);
                Assert.NotNull(readResults[i].Value!.Value);
                Assert.True(readResults[i].StatusCode.IsGood);

                Assert.Equal(_readVariables[i].type, readResults[i].Value!.Type);
                Assert.Equal(_readVariables[i].isArray, readResults[i].Value!.IsArray);

                if (_readVariables[i].isArray)
                {
                    Assert.Equal(3, ((Array)(readResults[i].Value!.Value)!).Length); // all test arrays are of length 3
                    return;
                }
            }
        }

        #endregion Connect and Read tests

        #region Connect and Write tests

        [Fact]
        public async Task Connect_And_Write_Anonymous_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.None;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodes = new NodeId[]
            {
                new(4, 226),
                new(4, 227)
            };

            // Read initial values
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var initScalarVal = readResults[0].Value;
            var initArrayVal = readResults[1].Value;

            Assert.NotNull(initScalarVal);
            Assert.False(initScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initScalarVal.Value);
            Assert.IsType<short>(initScalarVal.Value);

            Assert.NotNull(initArrayVal);
            Assert.True(initArrayVal.IsArray);
            Assert.Equal(3, ((Array)initArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initArrayVal.Value);
            Assert.IsType<short[]>(initArrayVal.Value);

            // Write new values
            var newScalarVal = new Variant(((short)((short)initScalarVal.Value + 1)), BuiltInType.Int16);
            var newArrayVal = new Variant(new short[]
            {
               (short)(((short[])initArrayVal.Value)[0] + 1),
               (short)(((short[])initArrayVal.Value)[1] + 1),
               (short)(((short[])initArrayVal.Value)[2] + 1),
            }, BuiltInType.Int16, true);

            // 2. Act
            var writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = newScalarVal },
                    new() { Value = newArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);
            Assert.True(writeResults[0].IsGood);
            Assert.True(writeResults[1].IsGood);

            // Read back values
            readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var readBackScalarVal = readResults[0].Value;
            var readBackArrayVal = readResults[1].Value;

            Assert.NotNull(readBackScalarVal);
            Assert.False(readBackScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, readBackScalarVal.Type);
            Assert.NotNull(readBackScalarVal.Value);
            Assert.IsType<short>(readBackScalarVal.Value);
            Assert.Equal(newScalarVal.Value, readBackScalarVal.Value);

            Assert.NotNull(readBackArrayVal);
            Assert.True(readBackArrayVal.IsArray);
            Assert.Equal(3, ((Array)readBackArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, readBackArrayVal.Type);
            Assert.NotNull(readBackArrayVal.Value);
            Assert.IsType<short[]>(readBackArrayVal.Value);

            for (int i = 0; i < ((Array)readBackArrayVal.Value).Length; i++)
            {
                Assert.Equal(((short[])newArrayVal.Value!)[i], ((short[])readBackArrayVal.Value)[i]);
            }

            // Write back initial values

            writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = initScalarVal },
                    new() { Value = initArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);

            foreach (var res in writeResults)
            {
                Assert.True(res.IsGood);
            }
        }

        [Fact]
        public async Task Connect_And_Write_Anonymous_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodes = new NodeId[]
            {
                new(4, 226),
                new(4, 227)
            };

            // Read initial values
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var initScalarVal = readResults[0].Value;
            var initArrayVal = readResults[1].Value;

            Assert.NotNull(initScalarVal);
            Assert.False(initScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initScalarVal.Value);
            Assert.IsType<short>(initScalarVal.Value);

            Assert.NotNull(initArrayVal);
            Assert.True(initArrayVal.IsArray);
            Assert.Equal(3, ((Array)initArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initArrayVal.Value);
            Assert.IsType<short[]>(initArrayVal.Value);

            // Write new values
            var newScalarVal = new Variant(((short)((short)initScalarVal.Value + 1)), BuiltInType.Int16);
            var newArrayVal = new Variant(new short[]
            {
               (short)(((short[])initArrayVal.Value)[0] + 1),
               (short)(((short[])initArrayVal.Value)[1] + 1),
               (short)(((short[])initArrayVal.Value)[2] + 1),
            }, BuiltInType.Int16, true);

            // 2. Act
            var writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = newScalarVal },
                    new() { Value = newArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);
            Assert.True(writeResults[0].IsGood);
            Assert.True(writeResults[1].IsGood);

            // Read back values
            readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var readBackScalarVal = readResults[0].Value;
            var readBackArrayVal = readResults[1].Value;

            Assert.NotNull(readBackScalarVal);
            Assert.False(readBackScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, readBackScalarVal.Type);
            Assert.NotNull(readBackScalarVal.Value);
            Assert.IsType<short>(readBackScalarVal.Value);
            Assert.Equal(newScalarVal.Value, readBackScalarVal.Value);

            Assert.NotNull(readBackArrayVal);
            Assert.True(readBackArrayVal.IsArray);
            Assert.Equal(3, ((Array)readBackArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, readBackArrayVal.Type);
            Assert.NotNull(readBackArrayVal.Value);
            Assert.IsType<short[]>(readBackArrayVal.Value);

            for (int i = 0; i < ((Array)readBackArrayVal.Value).Length; i++)
            {
                Assert.Equal(((short[])newArrayVal.Value!)[i], ((short[])readBackArrayVal.Value)[i]);
            }

            // Write back initial values

            writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = initScalarVal },
                    new() { Value = initArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);

            foreach (var res in writeResults)
            {
                Assert.True(res.IsGood);
            }
        }

        [Fact]
        public async Task Connect_And_Write_Anonymous_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodes = new NodeId[]
            {
                new(4, 226),
                new(4, 227)
            };

            // Read initial values
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var initScalarVal = readResults[0].Value;
            var initArrayVal = readResults[1].Value;

            Assert.NotNull(initScalarVal);
            Assert.False(initScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initScalarVal.Value);
            Assert.IsType<short>(initScalarVal.Value);

            Assert.NotNull(initArrayVal);
            Assert.True(initArrayVal.IsArray);
            Assert.Equal(3, ((Array)initArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initArrayVal.Value);
            Assert.IsType<short[]>(initArrayVal.Value);

            // Write new values
            var newScalarVal = new Variant(((short)((short)initScalarVal.Value + 1)), BuiltInType.Int16);
            var newArrayVal = new Variant(new short[]
            {
               (short)(((short[])initArrayVal.Value)[0] + 1),
               (short)(((short[])initArrayVal.Value)[1] + 1),
               (short)(((short[])initArrayVal.Value)[2] + 1),
            }, BuiltInType.Int16, true);

            // 2. Act
            var writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = newScalarVal },
                    new() { Value = newArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);
            Assert.True(writeResults[0].IsGood);
            Assert.True(writeResults[1].IsGood);

            // Read back values
            readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var readBackScalarVal = readResults[0].Value;
            var readBackArrayVal = readResults[1].Value;

            Assert.NotNull(readBackScalarVal);
            Assert.False(readBackScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, readBackScalarVal.Type);
            Assert.NotNull(readBackScalarVal.Value);
            Assert.IsType<short>(readBackScalarVal.Value);
            Assert.Equal(newScalarVal.Value, readBackScalarVal.Value);

            Assert.NotNull(readBackArrayVal);
            Assert.True(readBackArrayVal.IsArray);
            Assert.Equal(3, ((Array)readBackArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, readBackArrayVal.Type);
            Assert.NotNull(readBackArrayVal.Value);
            Assert.IsType<short[]>(readBackArrayVal.Value);

            for (int i = 0; i < ((Array)readBackArrayVal.Value).Length; i++)
            {
                Assert.Equal(((short[])newArrayVal.Value!)[i], ((short[])readBackArrayVal.Value)[i]);
            }

            // Write back initial values

            writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = initScalarVal },
                    new() { Value = initArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);

            foreach (var res in writeResults)
            {
                Assert.True(res.IsGood);
            }
        }

        [Fact]
        public async Task Connect_And_Write_Username_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodes = new NodeId[]
            {
                new(4, 226),
                new(4, 227)
            };

            // Read initial values
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var initScalarVal = readResults[0].Value;
            var initArrayVal = readResults[1].Value;

            Assert.NotNull(initScalarVal);
            Assert.False(initScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initScalarVal.Value);
            Assert.IsType<short>(initScalarVal.Value);

            Assert.NotNull(initArrayVal);
            Assert.True(initArrayVal.IsArray);
            Assert.Equal(3, ((Array)initArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initArrayVal.Value);
            Assert.IsType<short[]>(initArrayVal.Value);

            // Write new values
            var newScalarVal = new Variant(((short)((short)initScalarVal.Value + 1)), BuiltInType.Int16);
            var newArrayVal = new Variant(new short[]
            {
               (short)(((short[])initArrayVal.Value)[0] + 1),
               (short)(((short[])initArrayVal.Value)[1] + 1),
               (short)(((short[])initArrayVal.Value)[2] + 1),
            }, BuiltInType.Int16, true);

            // 2. Act
            var writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = newScalarVal },
                    new() { Value = newArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);
            Assert.True(writeResults[0].IsGood);
            Assert.True(writeResults[1].IsGood);

            // Read back values
            readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var readBackScalarVal = readResults[0].Value;
            var readBackArrayVal = readResults[1].Value;

            Assert.NotNull(readBackScalarVal);
            Assert.False(readBackScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, readBackScalarVal.Type);
            Assert.NotNull(readBackScalarVal.Value);
            Assert.IsType<short>(readBackScalarVal.Value);
            Assert.Equal(newScalarVal.Value, readBackScalarVal.Value);

            Assert.NotNull(readBackArrayVal);
            Assert.True(readBackArrayVal.IsArray);
            Assert.Equal(3, ((Array)readBackArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, readBackArrayVal.Type);
            Assert.NotNull(readBackArrayVal.Value);
            Assert.IsType<short[]>(readBackArrayVal.Value);

            for (int i = 0; i < ((Array)readBackArrayVal.Value).Length; i++)
            {
                Assert.Equal(((short[])newArrayVal.Value!)[i], ((short[])readBackArrayVal.Value)[i]);
            }

            // Write back initial values

            writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = initScalarVal },
                    new() { Value = initArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);

            foreach (var res in writeResults)
            {
                Assert.True(res.IsGood);
            }
        }

        [Fact]
        public async Task Connect_And_Write_Username_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodes = new NodeId[]
            {
                new(4, 226),
                new(4, 227)
            };

            // Read initial values
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var initScalarVal = readResults[0].Value;
            var initArrayVal = readResults[1].Value;

            Assert.NotNull(initScalarVal);
            Assert.False(initScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initScalarVal.Value);
            Assert.IsType<short>(initScalarVal.Value);

            Assert.NotNull(initArrayVal);
            Assert.True(initArrayVal.IsArray);
            Assert.Equal(3, ((Array)initArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initArrayVal.Value);
            Assert.IsType<short[]>(initArrayVal.Value);

            // Write new values
            var newScalarVal = new Variant(((short)((short)initScalarVal.Value + 1)), BuiltInType.Int16);
            var newArrayVal = new Variant(new short[]
            {
               (short)(((short[])initArrayVal.Value)[0] + 1),
               (short)(((short[])initArrayVal.Value)[1] + 1),
               (short)(((short[])initArrayVal.Value)[2] + 1),
            }, BuiltInType.Int16, true);

            // 2. Act
            var writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = newScalarVal },
                    new() { Value = newArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);
            Assert.True(writeResults[0].IsGood);
            Assert.True(writeResults[1].IsGood);

            // Read back values
            readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var readBackScalarVal = readResults[0].Value;
            var readBackArrayVal = readResults[1].Value;

            Assert.NotNull(readBackScalarVal);
            Assert.False(readBackScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, readBackScalarVal.Type);
            Assert.NotNull(readBackScalarVal.Value);
            Assert.IsType<short>(readBackScalarVal.Value);
            Assert.Equal(newScalarVal.Value, readBackScalarVal.Value);

            Assert.NotNull(readBackArrayVal);
            Assert.True(readBackArrayVal.IsArray);
            Assert.Equal(3, ((Array)readBackArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, readBackArrayVal.Type);
            Assert.NotNull(readBackArrayVal.Value);
            Assert.IsType<short[]>(readBackArrayVal.Value);

            for (int i = 0; i < ((Array)readBackArrayVal.Value).Length; i++)
            {
                Assert.Equal(((short[])newArrayVal.Value!)[i], ((short[])readBackArrayVal.Value)[i]);
            }

            // Write back initial values

            writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = initScalarVal },
                    new() { Value = initArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);

            foreach (var res in writeResults)
            {
                Assert.True(res.IsGood);
            }
        }

        [Fact]
        public async Task Connect_And_Write_Username_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodes = new NodeId[]
            {
                new(4, 226),
                new(4, 227)
            };

            // Read initial values
            await client.ConnectAsync();
            var readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var initScalarVal = readResults[0].Value;
            var initArrayVal = readResults[1].Value;

            Assert.NotNull(initScalarVal);
            Assert.False(initScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initScalarVal.Value);
            Assert.IsType<short>(initScalarVal.Value);

            Assert.NotNull(initArrayVal);
            Assert.True(initArrayVal.IsArray);
            Assert.Equal(3, ((Array)initArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, initScalarVal.Type);
            Assert.NotNull(initArrayVal.Value);
            Assert.IsType<short[]>(initArrayVal.Value);

            // Write new values
            var newScalarVal = new Variant(((short)((short)initScalarVal.Value + 1)), BuiltInType.Int16);
            var newArrayVal = new Variant(new short[]
            {
               (short)(((short[])initArrayVal.Value)[0] + 1),
               (short)(((short[])initArrayVal.Value)[1] + 1),
               (short)(((short[])initArrayVal.Value)[2] + 1),
            }, BuiltInType.Int16, true);

            // 2. Act
            var writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = newScalarVal },
                    new() { Value = newArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);
            Assert.True(writeResults[0].IsGood);
            Assert.True(writeResults[1].IsGood);

            // Read back values
            readResults = await client.ReadNodesAsync(nodes);

            Assert.NotNull(readResults);
            Assert.Equal(2, readResults.Length);
            Assert.True(readResults[0].StatusCode.IsGood);
            Assert.True(readResults[1].StatusCode.IsGood);

            var readBackScalarVal = readResults[0].Value;
            var readBackArrayVal = readResults[1].Value;

            Assert.NotNull(readBackScalarVal);
            Assert.False(readBackScalarVal.IsArray);
            Assert.Equal(BuiltInType.Int16, readBackScalarVal.Type);
            Assert.NotNull(readBackScalarVal.Value);
            Assert.IsType<short>(readBackScalarVal.Value);
            Assert.Equal(newScalarVal.Value, readBackScalarVal.Value);

            Assert.NotNull(readBackArrayVal);
            Assert.True(readBackArrayVal.IsArray);
            Assert.Equal(3, ((Array)readBackArrayVal.Value!).Length);
            Assert.Equal(BuiltInType.Int16, readBackArrayVal.Type);
            Assert.NotNull(readBackArrayVal.Value);
            Assert.IsType<short[]>(readBackArrayVal.Value);

            for (int i = 0; i < ((Array)readBackArrayVal.Value).Length; i++)
            {
                Assert.Equal(((short[])newArrayVal.Value!)[i], ((short[])readBackArrayVal.Value)[i]);
            }

            // Write back initial values

            writeResults = await client.WriteNodesAsync(nodes,
                [
                    new() { Value = initScalarVal },
                    new() { Value = initArrayVal }
                ]);

            Assert.NotNull(writeResults);
            Assert.Equal(2, writeResults.Length);

            foreach (var res in writeResults)
            {
                Assert.True(res.IsGood);
            }
        }

        #endregion Connect and Write tests

        #region Connect and Browse tests

        [Fact]
        public async Task Connect_And_Browse_Anonymous_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.None;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            // 2. Act
            await client.ConnectAsync();
            var result = await client.BrowseNodesAsync([new(0, 2253)]); // browsing "Server" root

            // 3. Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotNull(result[0]);
            Assert.True(result[0].Length > 1);
        }

        [Fact]
        public async Task Connect_And_Browse_Anonymous_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            // 2. Act
            await client.ConnectAsync();
            var result = await client.BrowseNodesAsync([new(0, 2253)]); // browsing "Server" root

            // 3. Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotNull(result[0]);
            Assert.True(result[0].Length > 1);
        }

        [Fact]
        public async Task Connect_And_Browse_Anonymous_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            // 2. Act
            await client.ConnectAsync();
            var result = await client.BrowseNodesAsync([new(0, 2253)]); // browsing "Server" root

            // 3. Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotNull(result[0]);
            Assert.True(result[0].Length > 1);
        }

        [Fact]
        public async Task Connect_And_Browse_Username_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            // 2. Act
            await client.ConnectAsync();
            var result = await client.BrowseNodesAsync([new(0, 2253)]); // browsing "Server" root

            // 3. Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotNull(result[0]);
            Assert.True(result[0].Length > 1);
        }

        [Fact]
        public async Task Connect_And_Browse_Username_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            // 2. Act
            await client.ConnectAsync();
            var result = await client.BrowseNodesAsync([new(0, 2253)]); // browsing "Server" root

            // 3. Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotNull(result[0]);
            Assert.True(result[0].Length > 1);
        }

        [Fact]
        public async Task Connect_And_Browse_Username_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            // 2. Act
            await client.ConnectAsync();
            var result = await client.BrowseNodesAsync([new(0, 2253)]); // browsing "Server" root

            // 3. Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotNull(result[0]);
            Assert.True(result[0].Length > 1);
        }

        #endregion Connect and Browse tests

        #region Connect and Call Untyped Method tests

        [Fact]
        public async Task Connect_And_Call_Untyped_Method_Anonymous_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.None;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var valAVariant = new Variant(valA, BuiltInType.Int16);
            var valBVariant = new Variant(valB, BuiltInType.Int16);

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync(objectNode, methodNode, default, valAVariant, valBVariant);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Single(outputArgs);
            Assert.NotNull(outputArgs[0].Value);
            Assert.IsType<int>(outputArgs[0].Value);
            Assert.Equal((int)(valA + valB), outputArgs[0].Value);
        }

        [Fact]
        public async Task Connect_And_Call_Untyped_Method_Anonymous_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var valAVariant = new Variant(valA, BuiltInType.Int16);
            var valBVariant = new Variant(valB, BuiltInType.Int16);

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync(objectNode, methodNode, default, valAVariant, valBVariant);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Single(outputArgs);
            Assert.NotNull(outputArgs[0].Value);
            Assert.IsType<int>(outputArgs[0].Value);
            Assert.Equal((int)(valA + valB), outputArgs[0].Value);
        }

        [Fact]
        public async Task Connect_And_Call_Untyped_Method_Anonymous_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var valAVariant = new Variant(valA, BuiltInType.Int16);
            var valBVariant = new Variant(valB, BuiltInType.Int16);

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync(objectNode, methodNode, default, valAVariant, valBVariant);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Single(outputArgs);
            Assert.NotNull(outputArgs[0].Value);
            Assert.IsType<int>(outputArgs[0].Value);
            Assert.Equal((int)(valA + valB), outputArgs[0].Value);
        }

        [Fact]
        public async Task Connect_And_Call_Untyped_Method_Username_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var valAVariant = new Variant(valA, BuiltInType.Int16);
            var valBVariant = new Variant(valB, BuiltInType.Int16);

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync(objectNode, methodNode, default, valAVariant, valBVariant);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Single(outputArgs);
            Assert.NotNull(outputArgs[0].Value);
            Assert.IsType<int>(outputArgs[0].Value);
            Assert.Equal((int)(valA + valB), outputArgs[0].Value);
        }

        [Fact]
        public async Task Connect_And_Call_Untyped_Method_Username_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var valAVariant = new Variant(valA, BuiltInType.Int16);
            var valBVariant = new Variant(valB, BuiltInType.Int16);

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync(objectNode, methodNode, default, valAVariant, valBVariant);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Single(outputArgs);
            Assert.NotNull(outputArgs[0].Value);
            Assert.IsType<int>(outputArgs[0].Value);
            Assert.Equal((int)(valA + valB), outputArgs[0].Value);
        }

        [Fact]
        public async Task Connect_And_Call_Untyped_Method_Username_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var valAVariant = new Variant(valA, BuiltInType.Int16);
            var valBVariant = new Variant(valB, BuiltInType.Int16);

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync(objectNode, methodNode, default, valAVariant, valBVariant);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Single(outputArgs);
            Assert.NotNull(outputArgs[0].Value);
            Assert.IsType<int>(outputArgs[0].Value);
            Assert.Equal((int)(valA + valB), outputArgs[0].Value);
        }

        #endregion Connect and Call Untyped Method tests

        #region Connect and Call Typed Method tests

        [Fact]
        public async Task Connect_And_Call_Typed_Method_Anonymous_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.None;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var inputArgs = new AddMethodInputArgs() { ValA = valA, ValB = valB };

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync<AddMethodInputArgs, AddMethodOutputArgs>(objectNode, methodNode, inputArgs);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Equal((int)(valA + valB), outputArgs.Result);
        }

        [Fact]
        public async Task Connect_And_Call_Typed_Method_Anonymous_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var inputArgs = new AddMethodInputArgs() { ValA = valA, ValB = valB };

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync<AddMethodInputArgs, AddMethodOutputArgs>(objectNode, methodNode, inputArgs);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Equal((int)(valA + valB), outputArgs.Result);
        }

        [Fact]
        public async Task Connect_And_Call_Typed_Method_Anonymous_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var inputArgs = new AddMethodInputArgs() { ValA = valA, ValB = valB };

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync<AddMethodInputArgs, AddMethodOutputArgs>(objectNode, methodNode, inputArgs);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Equal((int)(valA + valB), outputArgs.Result);
        }

        [Fact]
        public async Task Connect_And_Call_Typed_Method_Username_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var inputArgs = new AddMethodInputArgs() { ValA = valA, ValB = valB };

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync<AddMethodInputArgs, AddMethodOutputArgs>(objectNode, methodNode, inputArgs);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Equal((int)(valA + valB), outputArgs.Result);
        }

        [Fact]
        public async Task Connect_And_Call_Typed_Method_Username_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var inputArgs = new AddMethodInputArgs() { ValA = valA, ValB = valB };

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync<AddMethodInputArgs, AddMethodOutputArgs>(objectNode, methodNode, inputArgs);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Equal((int)(valA + valB), outputArgs.Result);
        }

        [Fact]
        public async Task Connect_And_Call_Typed_Method_Username_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var objectNode = new NodeId(4, 312);
            var methodNode = new NodeId(4, 313);
            var valA = (short)RandomNumberGenerator.GetInt32(0x7FFF);
            var valB = (short)RandomNumberGenerator.GetInt32(0x7FFF);

            var inputArgs = new AddMethodInputArgs() { ValA = valA, ValB = valB };

            // 2. Act
            await client.ConnectAsync();
            var outputArgs = await client.CallMethodAsync<AddMethodInputArgs, AddMethodOutputArgs>(objectNode, methodNode, inputArgs);

            // 3. Assert
            Assert.NotNull(outputArgs);
            Assert.Equal((int)(valA + valB), outputArgs.Result);
        }

        #endregion Connect and Call Typed Method tests

        #region Connect and Subscribe and Write Custom Struct Array tests

        [Fact]
        public async Task Connect_And_Subscribe_And_Write_Custom_Type_Anonymous_Unsecure()
        {
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.None;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodeId = new NodeId(4, 259);
            await client.ConnectAsync();

            // --- 1. Read initial values ---
            var readResults = await client.ReadNodesAsync([nodeId]);
            Assert.True(readResults![0].StatusCode.IsGood);
            var initialValue = readResults[0].Value;

            // --- 2. Prepare new Data ---
            static string GetRandomString() => Guid.NewGuid().ToString()[..10];

            var expectedStringKey = GetRandomString();

            var newExtObjArray = new ExtensionObject[]
            {
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = expectedStringKey
                }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                }
            };

            // --- 3. Setup Subscription & TaskCompletionSource ---
            var tcs = new TaskCompletionSource<ExtensionObject[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            await client.SubscribeAsync(
                [nodeId],
                (h, v) =>
                {
                    // ignore first (initial) call
                    if (v.Value!.Value is ExtensionObject[] arr && arr.Length > 0)
                    {
                        // compare with marker string
                        if (arr[0].DecodedValue is TestStruct firstItem && firstItem.TestStructString == expectedStringKey)
                        {
                            tcs.TrySetResult(arr);
                        }
                    }
                });

            // --- 4. Write new values ---
            var writeResults = await client.WriteNodesAsync([nodeId],
                [
                    new() { Value = new Variant(newExtObjArray, BuiltInType.ExtensionObject, true)}
                ]);

            Assert.True(writeResults![0].IsGood);

            // --- 5. Await the change ---
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));

            if (completedTask != tcs.Task)
            {
                throw new TimeoutException();
            }

            var newResults = await tcs.Task;

            Assert.NotNull(newResults);
            Assert.Equal(newExtObjArray.Length, newResults.Length);

            for (int i = 0; i < newResults.Length; i++)
            {
                var expected = (TestStruct)newExtObjArray[i].DecodedValue!;
                var actual = (TestStruct)newResults[i].DecodedValue!;

                Assert.Equal(expected.TestStructBool, actual.TestStructBool);
                Assert.Equal(expected.TestStructInt, actual.TestStructInt);
                Assert.Equal(expected.TestStructString, actual.TestStructString);
            }

            // --- 7. Reset values (Cleanup) ---
            await client.WriteNodesAsync([nodeId], [new DataValue() { Value = initialValue }]);
        }

        [Fact]
        public async Task Connect_And_Subscribe_And_Write_Custom_Type_Anonymous_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodeId = new NodeId(4, 259);
            await client.ConnectAsync();

            // --- 1. Read initial values ---
            var readResults = await client.ReadNodesAsync([nodeId]);
            Assert.True(readResults![0].StatusCode.IsGood);
            var initialValue = readResults[0].Value;

            // --- 2. Prepare new Data ---
            static string GetRandomString() => Guid.NewGuid().ToString()[..10];

            var expectedStringKey = GetRandomString();

            var newExtObjArray = new ExtensionObject[]
            {
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = expectedStringKey
                }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                }
            };

            // --- 3. Setup Subscription & TaskCompletionSource ---
            var tcs = new TaskCompletionSource<ExtensionObject[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            await client.SubscribeAsync(
                [nodeId],
                (h, v) =>
                {
                    // ignore first (initial) call
                    if (v.Value!.Value is ExtensionObject[] arr && arr.Length > 0)
                    {
                        // compare with marker string
                        if (arr[0].DecodedValue is TestStruct firstItem && firstItem.TestStructString == expectedStringKey)
                        {
                            tcs.TrySetResult(arr);
                        }
                    }
                });

            // --- 4. Write new values ---
            var writeResults = await client.WriteNodesAsync([nodeId],
                [
                    new() { Value = new Variant(newExtObjArray, BuiltInType.ExtensionObject, true)}
                ]);

            Assert.True(writeResults![0].IsGood);

            // --- 5. Await the change ---
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));

            if (completedTask != tcs.Task)
            {
                throw new TimeoutException();
            }

            var newResults = await tcs.Task;

            Assert.NotNull(newResults);
            Assert.Equal(newExtObjArray.Length, newResults.Length);

            for (int i = 0; i < newResults.Length; i++)
            {
                var expected = (TestStruct)newExtObjArray[i].DecodedValue!;
                var actual = (TestStruct)newResults[i].DecodedValue!;

                Assert.Equal(expected.TestStructBool, actual.TestStructBool);
                Assert.Equal(expected.TestStructInt, actual.TestStructInt);
                Assert.Equal(expected.TestStructString, actual.TestStructString);
            }

            // --- 7. Reset values (Cleanup) ---
            await client.WriteNodesAsync([nodeId], [new DataValue() { Value = initialValue }]);
        }

        [Fact]
        public async Task Connect_And_Subscribe_And_Write_Custom_Type_Anonymous_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodeId = new NodeId(4, 259);
            await client.ConnectAsync();

            // --- 1. Read initial values ---
            var readResults = await client.ReadNodesAsync([nodeId]);
            Assert.True(readResults![0].StatusCode.IsGood);
            var initialValue = readResults[0].Value;

            // --- 2. Prepare new Data ---
            static string GetRandomString() => Guid.NewGuid().ToString()[..10];

            var expectedStringKey = GetRandomString();

            var newExtObjArray = new ExtensionObject[]
            {
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = expectedStringKey
                }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                }
            };

            // --- 3. Setup Subscription & TaskCompletionSource ---
            var tcs = new TaskCompletionSource<ExtensionObject[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            await client.SubscribeAsync(
                [nodeId],
                (h, v) =>
                {
                    // ignore first (initial) call
                    if (v.Value!.Value is ExtensionObject[] arr && arr.Length > 0)
                    {
                        // compare with marker string
                        if (arr[0].DecodedValue is TestStruct firstItem && firstItem.TestStructString == expectedStringKey)
                        {
                            tcs.TrySetResult(arr);
                        }
                    }
                });

            // --- 4. Write new values ---
            var writeResults = await client.WriteNodesAsync([nodeId],
                [
                    new() { Value = new Variant(newExtObjArray, BuiltInType.ExtensionObject, true)}
                ]);

            Assert.True(writeResults![0].IsGood);

            // --- 5. Await the change ---
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));

            if (completedTask != tcs.Task)
            {
                throw new TimeoutException();
            }

            var newResults = await tcs.Task;

            Assert.NotNull(newResults);
            Assert.Equal(newExtObjArray.Length, newResults.Length);

            for (int i = 0; i < newResults.Length; i++)
            {
                var expected = (TestStruct)newExtObjArray[i].DecodedValue!;
                var actual = (TestStruct)newResults[i].DecodedValue!;

                Assert.Equal(expected.TestStructBool, actual.TestStructBool);
                Assert.Equal(expected.TestStructInt, actual.TestStructInt);
                Assert.Equal(expected.TestStructString, actual.TestStructString);
            }

            // --- 7. Reset values (Cleanup) ---
            await client.WriteNodesAsync([nodeId], [new DataValue() { Value = initialValue }]);
        }

        [Fact]
        public async Task Connect_And_Subscribe_And_Write_Custom_Type_Username_Unsecure()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.PolicyType = SecurityPolicyType.None;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodeId = new NodeId(4, 259);
            await client.ConnectAsync();

            // --- 1. Read initial values ---
            var readResults = await client.ReadNodesAsync([nodeId]);
            Assert.True(readResults![0].StatusCode.IsGood);
            var initialValue = readResults[0].Value;

            // --- 2. Prepare new Data ---
            static string GetRandomString() => Guid.NewGuid().ToString()[..10];

            var expectedStringKey = GetRandomString();

            var newExtObjArray = new ExtensionObject[]
            {
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = expectedStringKey
                }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                }
            };

            // --- 3. Setup Subscription & TaskCompletionSource ---
            var tcs = new TaskCompletionSource<ExtensionObject[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            await client.SubscribeAsync(
                [nodeId],
                (h, v) =>
                {
                    // ignore first (initial) call
                    if (v.Value!.Value is ExtensionObject[] arr && arr.Length > 0)
                    {
                        // compare with marker string
                        if (arr[0].DecodedValue is TestStruct firstItem && firstItem.TestStructString == expectedStringKey)
                        {
                            tcs.TrySetResult(arr);
                        }
                    }
                });

            // --- 4. Write new values ---
            var writeResults = await client.WriteNodesAsync([nodeId],
                [
                    new() { Value = new Variant(newExtObjArray, BuiltInType.ExtensionObject, true)}
                ]);

            Assert.True(writeResults![0].IsGood);

            // --- 5. Await the change ---
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));

            if (completedTask != tcs.Task)
            {
                throw new TimeoutException();
            }

            var newResults = await tcs.Task;

            Assert.NotNull(newResults);
            Assert.Equal(newExtObjArray.Length, newResults.Length);

            for (int i = 0; i < newResults.Length; i++)
            {
                var expected = (TestStruct)newExtObjArray[i].DecodedValue!;
                var actual = (TestStruct)newResults[i].DecodedValue!;

                Assert.Equal(expected.TestStructBool, actual.TestStructBool);
                Assert.Equal(expected.TestStructInt, actual.TestStructInt);
                Assert.Equal(expected.TestStructString, actual.TestStructString);
            }

            // --- 7. Reset values (Cleanup) ---
            await client.WriteNodesAsync([nodeId], [new DataValue() { Value = initialValue }]);
        }

        [Fact]
        public async Task Connect_And_Subscribe_And_Write_Custom_Type_Username_Sign()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.Sign;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodeId = new NodeId(4, 259);
            await client.ConnectAsync();

            // --- 1. Read initial values ---
            var readResults = await client.ReadNodesAsync([nodeId]);
            Assert.True(readResults![0].StatusCode.IsGood);
            var initialValue = readResults[0].Value;

            // --- 2. Prepare new Data ---
            static string GetRandomString() => Guid.NewGuid().ToString()[..10];

            var expectedStringKey = GetRandomString();

            var newExtObjArray = new ExtensionObject[]
            {
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = expectedStringKey
                }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                }
            };

            // --- 3. Setup Subscription & TaskCompletionSource ---
            var tcs = new TaskCompletionSource<ExtensionObject[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            await client.SubscribeAsync(
                [nodeId],
                (h, v) =>
                {
                    // ignore first (initial) call
                    if (v.Value!.Value is ExtensionObject[] arr && arr.Length > 0)
                    {
                        // compare with marker string
                        if (arr[0].DecodedValue is TestStruct firstItem && firstItem.TestStructString == expectedStringKey)
                        {
                            tcs.TrySetResult(arr);
                        }
                    }
                });

            // --- 4. Write new values ---
            var writeResults = await client.WriteNodesAsync([nodeId],
                [
                    new() { Value = new Variant(newExtObjArray, BuiltInType.ExtensionObject, true)}
                ]);

            Assert.True(writeResults![0].IsGood);

            // --- 5. Await the change ---
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));

            if (completedTask != tcs.Task)
            {
                throw new TimeoutException();
            }

            var newResults = await tcs.Task;

            Assert.NotNull(newResults);
            Assert.Equal(newExtObjArray.Length, newResults.Length);

            for (int i = 0; i < newResults.Length; i++)
            {
                var expected = (TestStruct)newExtObjArray[i].DecodedValue!;
                var actual = (TestStruct)newResults[i].DecodedValue!;

                Assert.Equal(expected.TestStructBool, actual.TestStructBool);
                Assert.Equal(expected.TestStructInt, actual.TestStructInt);
                Assert.Equal(expected.TestStructString, actual.TestStructString);
            }

            // --- 7. Reset values (Cleanup) ---
            await client.WriteNodesAsync([nodeId], [new DataValue() { Value = initialValue }]);
        }

        [Fact]
        public async Task Connect_And_Subscribe_And_Write_Custom_Type_Username_SignAndEncrypt()
        {
            // 1. Arrange
            await using var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.AutoAcceptUntrustedCertificates = true;
                    s.ClientCertificate = _testCertificate;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenPolicyType = SecurityPolicyType.Basic256Sha256;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = TestUser;
                    s.Password = TestPassword;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                })
                .Build();

            var nodeId = new NodeId(4, 259);
            await client.ConnectAsync();

            // --- 1. Read initial values ---
            var readResults = await client.ReadNodesAsync([nodeId]);
            Assert.True(readResults![0].StatusCode.IsGood);
            var initialValue = readResults[0].Value;

            // --- 2. Prepare new Data ---
            static string GetRandomString() => Guid.NewGuid().ToString()[..10];

            var expectedStringKey = GetRandomString();

            var newExtObjArray = new ExtensionObject[]
            {
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = expectedStringKey
                }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                },
                new()
                {
                    DecodedValue = new TestStruct() {
                    TestStructBool = RandomNumberGenerator.GetInt32(2) > 0,
                    TestStructInt = (short)RandomNumberGenerator.GetInt32(0x7FFF),
                    TestStructString = GetRandomString()
                    }
                }
            };

            // --- 3. Setup Subscription & TaskCompletionSource ---
            var tcs = new TaskCompletionSource<ExtensionObject[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            await client.SubscribeAsync(
                [nodeId],
                (h, v) =>
                {
                    // ignore first (initial) call
                    if (v.Value!.Value is ExtensionObject[] arr && arr.Length > 0)
                    {
                        // compare with marker string
                        if (arr[0].DecodedValue is TestStruct firstItem && firstItem.TestStructString == expectedStringKey)
                        {
                            tcs.TrySetResult(arr);
                        }
                    }
                });

            // --- 4. Write new values ---
            var writeResults = await client.WriteNodesAsync([nodeId],
                [
                    new() { Value = new Variant(newExtObjArray, BuiltInType.ExtensionObject, true)}
                ]);

            Assert.True(writeResults![0].IsGood);

            // --- 5. Await the change ---
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));

            if (completedTask != tcs.Task)
            {
                throw new TimeoutException();
            }

            var newResults = await tcs.Task;

            Assert.NotNull(newResults);
            Assert.Equal(newExtObjArray.Length, newResults.Length);

            for (int i = 0; i < newResults.Length; i++)
            {
                var expected = (TestStruct)newExtObjArray[i].DecodedValue!;
                var actual = (TestStruct)newResults[i].DecodedValue!;

                Assert.Equal(expected.TestStructBool, actual.TestStructBool);
                Assert.Equal(expected.TestStructInt, actual.TestStructInt);
                Assert.Equal(expected.TestStructString, actual.TestStructString);
            }

            // --- 7. Reset values (Cleanup) ---
            await client.WriteNodesAsync([nodeId], [new DataValue() { Value = initialValue }]);
        }

        #endregion Connect and Subscribe and Write Custom Struct Array tests

        #region Helpers

        private readonly (NodeId nodeId, BuiltInType type, bool isArray)[] _readVariables =
            [
                (new(4, 3),     BuiltInType.Boolean,            false),
                (new(4, 4),     BuiltInType.Boolean,            true),
                (new(4, 8),     BuiltInType.Byte,               false),
                (new(4, 9),     BuiltInType.Byte,               true),
                (new(4, 13),    BuiltInType.Byte,               false),
                (new(4, 14),    BuiltInType.Byte,               true),
                (new(4, 18),    BuiltInType.Int32,              false),
                (new(4, 19),    BuiltInType.Int32,              true),
                (new(4, 25),    BuiltInType.ExtensionObject,    false),
                (new(4, 34),    BuiltInType.ExtensionObject,    true),
                (new(4, 62),    BuiltInType.UInt32,             false),
                (new(4, 63),    BuiltInType.UInt32,             true),
                (new(4, 67),    BuiltInType.UInt16,             false),
                (new(4, 68),    BuiltInType.UInt16,             true),
                (new(4, 72),    BuiltInType.Int16,              false),
                (new(4, 73),    BuiltInType.Int16,              true),
                (new(4, 77),    BuiltInType.Double,             false),
                (new(4, 78),    BuiltInType.Double,             true),
                (new(4, 82),    BuiltInType.Float,              false),
                (new(4, 83),    BuiltInType.Float,              true),
                (new(4, 87),    BuiltInType.SByte,              false),
                (new(4, 88),    BuiltInType.SByte,              true),
                (new(4, 92),    BuiltInType.String,             false),
                (new(4, 93),    BuiltInType.String,             true),
                (new(4, 99),    BuiltInType.ExtensionObject,    false),
                (new(4, 105),   BuiltInType.ExtensionObject,    true),
                (new(4, 118),   BuiltInType.Int32,              false),
                (new(4, 119),   BuiltInType.Int32,              true),
                (new(4, 123),   BuiltInType.UInt32,             false),
                (new(4, 124),   BuiltInType.UInt32,             true),
                (new(4, 128),   BuiltInType.UInt32,             false),
                (new(4, 129),   BuiltInType.UInt32,             true),
                (new(4, 133),   BuiltInType.UInt16,             false),
                (new(4, 134),   BuiltInType.UInt16,             true),
                (new(4, 138),   BuiltInType.Byte,               false),
                (new(4, 139),   BuiltInType.Byte,               true),
                (new(4, 143),   BuiltInType.UInt16,             false),
                (new(4, 144),   BuiltInType.UInt16,             true),
                (new(4, 148),   BuiltInType.String,             false),
                (new(4, 149),   BuiltInType.String,             true),
                (new(4, 153),   BuiltInType.UInt16,             false),
                (new(4, 154),   BuiltInType.UInt16,             true),
            ];

        private class AddMethodInputArgs
        {
            [OpcMethodParameter(0, BuiltInType.Int16)]
            public short ValA { get; set; }

            [OpcMethodParameter(1, BuiltInType.Int16)]
            public short ValB { get; set; }
        }

        private class AddMethodOutputArgs
        {
            [OpcMethodParameter(0, BuiltInType.Int32)]
            public int Result { get; set; }
        }

        private class TestStruct
        {
            public bool TestStructBool { get; set; }
            public short TestStructInt { get; set; }
            public string TestStructString { get; set; } = string.Empty;

            public static TestStruct Decode(OpcUaBinaryReader reader)
            {
                var testStruct = new TestStruct
                {
                    TestStructBool = reader.ReadBoolean(),
                    TestStructInt = reader.ReadInt16(),
                    TestStructString = reader.ReadString() ?? string.Empty
                };

                return testStruct;
            }

            public void Encode(OpcUaBinaryWriter writer)
            {
                writer.WriteBoolean(TestStructBool);
                writer.WriteInt16(TestStructInt);
                writer.WriteString(TestStructString);
            }
        }

        #endregion Helpers
    }
}