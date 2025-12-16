using LiteUa.BuiltIn;
using LiteUa.Client;
using LiteUa.Encoding;
using LiteUa.Security;
using LiteUa.Security.Policies;
using LiteUa.Stack.Attribute;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.Method;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Stack.Subscription;
using LiteUa.Stack.Subscription.MonitoredItem;
using LiteUa.Transport;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit.Abstractions;

namespace LiteUa.Tests.IntegrationTests.Client
{
    public class UaClient_IntegrationTest_S7_1200
    {
        private readonly ITestOutputHelper _output;
        public const string ServerUrl = "opc.tcp://192.178.0.1:4840/";

        public UaClient_IntegrationTest_S7_1200(ITestOutputHelper output)
        {
            _output = output;
            RegisterCustomTypes();
        }

        private static void RegisterCustomTypes()
        {
            //CustomUaTypeRegistry.Register<TestStruct>(new NodeId(4, 25), TestStruct.Decode, (val, w) => val.Encode(w));
            //CustomUaTypeRegistry.Register<TestStruct>(new NodeId(4, 98), TestStruct.Decode, (val, w) => val.Encode(w));
            //CustomUaTypeRegistry.Register<TestStruct>(new NodeId(4, 104), TestStruct.Decode, (val, w) => val.Encode(w));
        }

        [Fact]
        public async Task Connect_Anonymously_Without_Security_And_Read_Node()
        {
            // 1. Arrange
            var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.PolicyType = SecurityPolicyType.None;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                });

            await using var uaClient = client.Build();
            await uaClient.Connect();

            // 2. Act
            var result = await uaClient.ReadNodesAsync([new NodeId(0, 2254)]); // server array

            // 3. Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains("urn:SIMATIC.S7-1200.OPC-UA.Application", ((string?[]?)result[0].Value?.Value)?[0]);
        }

        [Fact]
        public async Task Connect_And_Login_With_Security_And_Read_Node()
        {
            // 1. Arrange
            var cert = CertificateFactory.CreateSelfSignedCertificate("LiteUa Client", "urn:LiteUa:client");

            var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.PolicyType = SecurityPolicyType.Basic256Sha256;
                    s.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
                    s.UserTokenType = UserTokenType.Username;
                    s.Username = "username";
                    s.Password = "Password1"; // you don't even have to try ;-)
                    s.ClientCertificate = cert;
                    s.AutoAcceptUntrustedCertificates = true;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                });

            await using var uaClient = client.Build();
            await uaClient.Connect();

            // 2. Act
            var result = await uaClient.ReadNodesAsync([new NodeId(0, 2254)]); // server array

            // 3. Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains("urn:SIMATIC.S7-1200.OPC-UA.Application", ((string?[]?)result[0].Value?.Value)?[0]);
        }

        [Fact]
        public async Task Connect_And_Bulk_Read_Nodes()
        {
            // 1. Arrange
            var client = UaClient.Create()
                .ForEndpoint(ServerUrl)
                .WithSecurity(s =>
                {
                    s.PolicyType = SecurityPolicyType.None;
                    s.MessageSecurityMode = MessageSecurityMode.None;
                    s.UserTokenType = UserTokenType.Anonymous;
                })
                .WithSession(s =>
                {
                })
                .WithPool(p =>
                {
                    p.MaxSize = 1;
                });

            await using var uaClient = client.Build();

            var nodesToRead = new NodeId[]
            {
                new(2271), // LocaleIdArray
                new(11702), // MaxArrayLength
                new(12911) // MaxByteStringLength
            };

            // 2. Act
            await uaClient.Connect();
            var results = await uaClient.ReadNodesAsync(nodesToRead); // server namespace array

            // 3. Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            Assert.Equal(3, results.Length);
            Assert.NotNull(results[0]);
            Assert.NotNull(results[1]);
            Assert.NotNull(results[2]);
            Assert.True(results[0].StatusCode.IsGood);
            Assert.True(results[1].StatusCode.IsGood);
            Assert.True(results[2].StatusCode.IsGood);
        }

        /*
        [Fact]
        public async Task Establish_Secure_Channel_Basic256Sha256_SignAndEncrypt()
        {
            // ------------------------------------------------------------
            // 1. Discovery without encryption
            // ------------------------------------------------------------
            EndpointDescription? targetEndpoint = null;

            await using (var discoveryClient = new UaTcpClientChannel(TestServerUrl))
            {
                await discoveryClient.ConnectAsync();
                var endpointsResponse = await discoveryClient.GetEndpointsAsync();

                // Search for Basic256Sha256 with SignAndEncrypt
                targetEndpoint = endpointsResponse?.Endpoints?.FirstOrDefault(e =>
                    e.SecurityPolicyUri == "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256" &&
                    e.SecurityMode == MessageSecurityMode.SignAndEncrypt);
            }

            Assert.NotNull(targetEndpoint);

            // ------------------------------------------------------------
            // 2. Certificates
            // ------------------------------------------------------------

            var serverCert = X509CertificateLoader.LoadCertificate(targetEndpoint.ServerCertificate!);
            var clientCert = CertificateFactory.CreateSelfSignedCertificate("S7NexusClient", "urn:localhost:S7NexusClient");

            // ------------------------------------------------------------
            // 3. Security Policy
            // ------------------------------------------------------------
            var policy = new SecurityPolicyBasic256Sha256(clientCert, serverCert);

            // ------------------------------------------------------------
            // 4. Connect using encryption
            // ------------------------------------------------------------

            string connectUrl = TestServerUrl;

            await using var secureClient = new UaTcpClientChannel(
                connectUrl,
                policy,
                clientCert,
                serverCert,
                MessageSecurityMode.SignAndEncrypt);
            // Handshake (Asymmetric Encrypt/Sign)
            await secureClient.ConnectAsync();
            Assert.NotEqual((uint)0, secureClient.SecureChannelId);

            // ------------------------------------------------------------
            // 5. Send payload (Symmetric Encrypt/Sign)
            // ------------------------------------------------------------
            var response = await secureClient.GetEndpointsAsync();

            Assert.NotNull(response);
            Assert.NotNull(response.Endpoints);
            Assert.NotEmpty(response.Endpoints);
        }

        [Fact]
        public async Task Create_Anynomous_Session()
        {
            // --- 1. Preparation & Discovery ---
            string appUri = "urn:localhost:S7NexusClient";

            EndpointDescription? targetEndpoint = null;
            UserTokenPolicy? anonTokenPolicy = null;

            await using (var discovery = new UaTcpClientChannel(TestServerUrl))
            {
                await discovery.ConnectAsync();
                var endpoints = await discovery.GetEndpointsAsync();

                targetEndpoint = endpoints.Endpoints?.FirstOrDefault(e =>
                    e.SecurityPolicyUri == "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256" &&
                    e.SecurityMode == MessageSecurityMode.SignAndEncrypt);

                Assert.NotNull(targetEndpoint);

                // PolicyID for Anonymous (TokenType = 0)
                anonTokenPolicy = targetEndpoint.UserIdentityTokens?.FirstOrDefault(t => t.TokenType == 0); // 0 = Anonymous
            }

            Assert.NotNull(anonTokenPolicy);
            Assert.NotNull(anonTokenPolicy.PolicyId);

            // --- 2. Certificates ---
            var serverCert = X509CertificateLoader.LoadCertificate(targetEndpoint.ServerCertificate!);
            var clientCert = CertificateFactory.CreateSelfSignedCertificate("S7NexusClient", appUri);
            var policy = new SecurityPolicyBasic256Sha256(clientCert, serverCert);

            // --- 3. Secure Connect & Login ---
            await using var client = new UaTcpClientChannel(TestServerUrl, policy, clientCert, serverCert, MessageSecurityMode.SignAndEncrypt);
            await client.ConnectAsync();

            // Create Session
            await client.CreateSessionAsync(appUri, "urn:S7Nexus:Lib", "AnonymousSession");

            // Create Identity
            var identity = new AnonymousIdentity(anonTokenPolicy.PolicyId);

            // Login
            await client.ActivateSessionAsync(identity);

            // Verify: Send a Request
            var response = await client.GetEndpointsAsync();

            Assert.NotNull(response);
            Assert.NotNull(response.Endpoints);
            Assert.NotEmpty(response.Endpoints);
        }

        [Fact]
        public async Task Login_With_Username_And_Password()
        {
            // 1. Discovery
            EndpointDescription? targetEndpoint = null;
            UserTokenPolicy? userTokenPolicy = null;

            await using (var discovery = new UaTcpClientChannel(TestServerUrl))
            {
                await discovery.ConnectAsync();
                var endpoints = await discovery.GetEndpointsAsync();

                targetEndpoint = endpoints.Endpoints?.FirstOrDefault(e =>
                    e.SecurityPolicyUri == "http://opcfoundation.org/UA/SecurityPolicy#None");

                Assert.NotNull(targetEndpoint);
                userTokenPolicy = targetEndpoint.UserIdentityTokens?.FirstOrDefault(t =>
                    t.TokenType == 1 && // UserName
                    t.SecurityPolicyUri == "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256"
                );

                if (userTokenPolicy == null || userTokenPolicy.PolicyId == null) throw new Exception("Server does not offer Encrypted Password Login on None Channel");
            }

            // 2. Certificates
            if (targetEndpoint.ServerCertificate == null || targetEndpoint.ServerCertificate.Length == 0)
                throw new Exception("Server Endpoint 'None' did not return a Certificate (required for Password Encryption)");

            var serverCert = X509CertificateLoader.LoadCertificate(targetEndpoint.ServerCertificate!);
            var clientCert = CertificateFactory.CreateSelfSignedCertificate("S7NexusClient", "urn:localhost:S7NexusClient");

            // 3. Channel Setup
            var channelPolicy = new SecurityPolicyBasic256Sha256(clientCert, serverCert);
            await using var client = new UaTcpClientChannel(
                TestServerUrl,
                channelPolicy,
                clientCert,
                serverCert,
                MessageSecurityMode.SignAndEncrypt);
            await client.ConnectAsync();

            // Create Session
            await client.CreateSessionAsync("urn:localhost:S7NexusClient", "urn:S7Nexus:Lib", "MixedSession");

            // 4. Login
            var identity = new UserNameIdentity(userTokenPolicy.PolicyId, "username", "password");
            await client.ActivateSessionAsync(identity);

            // Verify: Send a Request
            var response = await client.GetEndpointsAsync();

            Assert.NotNull(response);
            Assert.NotNull(response.Endpoints);
            Assert.NotEmpty(response.Endpoints);
        }

        [Fact]
        public async Task Read_All_S7_Datatypes_Correctly()
        {
            var testConfigs = new (string Name, uint NodeId, BuiltInType Type, bool IsArray)[]
            {
                ("TestBool",        3, BuiltInType.Boolean, false),
                ("TestBoolArray",   4, BuiltInType.Boolean, true),
                ("TestByte",        8, BuiltInType.Byte,    false),
                ("TestByteArray",   9, BuiltInType.Byte,    true),
                ("TestChar",        13, BuiltInType.Byte,    false),
                ("TestCharArray",   14, BuiltInType.Byte,    true),
                ("TestDInt",        18, BuiltInType.Int32,   false),
                ("TestDIntArray",   19, BuiltInType.Int32,   true),
                ("TestDTL",         25, BuiltInType.ExtensionObject, false),
                ("TestDTLArray",    34, BuiltInType.ExtensionObject, true),
                ("TestDWord",       62, BuiltInType.UInt32,  false),
                ("TestDWordArray",  63, BuiltInType.UInt32,  true),
                ("TestDate",        67, BuiltInType.UInt16,  false),
                ("TestDateArray",   68, BuiltInType.UInt16,  true),
                ("TestInt",         72, BuiltInType.Int16,   false),
                ("TestIntArray",    73, BuiltInType.Int16,   true),
                ("TestLReal",       77, BuiltInType.Double,  false),
                ("TestLRealArray",  78, BuiltInType.Double,  true),
                ("TestReal",        82, BuiltInType.Float,   false),
                ("TestRealArray",   83, BuiltInType.Float,   true),
                ("TestSInt",        87, BuiltInType.SByte,   false),
                ("TestSIntArray",   88, BuiltInType.SByte,   true),
                ("TestString",      92, BuiltInType.String,  false),
                ("TestStringArray", 93, BuiltInType.String,  true),
                ("TestStruct",      99, BuiltInType.ExtensionObject, false),
                ("TestStructArray", 105, BuiltInType.ExtensionObject, true),
                ("TestTime",        118, BuiltInType.Int32,   false),
                ("TestTimeArray",   119, BuiltInType.Int32,   true),
                ("TestTimeOfDay",   123, BuiltInType.UInt32,  false),
                ("TestTimeOfDayArray", 124, BuiltInType.UInt32, true),
                ("TestUDInt",       128, BuiltInType.UInt32,  false),
                ("TestUDIntArray",  129, BuiltInType.UInt32,  true),
                ("TestUInt",        133, BuiltInType.UInt16,  false),
                ("TestUIntArray",   134, BuiltInType.UInt16,  true),
                ("TestUSInt",       138, BuiltInType.Byte,    false),
                ("TestUSIntArray",  139, BuiltInType.Byte,    true),
                ("TestWChar",       143, BuiltInType.UInt16,  false),
                ("TestWCharArray",  144, BuiltInType.UInt16,  true),
                ("TestWString",     148, BuiltInType.String,  false),
                ("TestWStringArray",149, BuiltInType.String,  true),
                ("TestWord",        153, BuiltInType.UInt16,  false),
                ("TestWordArray",   154, BuiltInType.UInt16,  true),
            };

            var nodesToRead = new NodeId[testConfigs.Length];
            for (int i = 0; i < testConfigs.Length; i++)
            {
                nodesToRead[i] = new NodeId(4, testConfigs[i].NodeId);
            }

            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:s7nexus:client", "urn:s7nexus", "S7BulkRead");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            var results = await client.ReadAsync(nodesToRead);

            Assert.NotNull(results);
            Assert.NotEmpty(results);
            Assert.Equal(testConfigs.Length, results.Length);

            for (int i = 0; i < results.Length; i++)
            {
                var result = results[i];
                var config = testConfigs[i];

                if (!result.StatusCode.IsGood)
                {
                    throw new Exception($"Bad StatusCode for {config.Name}: {result.StatusCode}");
                }

                if (result.Value?.Type != config.Type)
                {
                    throw new Exception($"Type Mismatch for {config.Name}. Expected {config.Type}, Got {result.Value?.Type}");
                }

                if (result.Value.IsArray != config.IsArray)
                {
                    throw new Exception($"Array Flag Mismatch for {config.Name}. Expected {config.IsArray}, Got {result.Value.IsArray}");
                }

                if (config.IsArray)
                {
                    var arr = result.Value.Value as Array;
                    Assert.Equal(3, arr?.Length);
                }
            }
        }

        [Fact]
        public async Task Read_And_Decode_Custom_S7DTL()
        {
            NodeId dtlNodeId = new(4, 25);
            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:s7nexus:client", "urn:s7nexus", "TypeTest");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            // 1. Read
            var results = await client.ReadAsync([dtlNodeId]);
            var extObj = results?[0].Value?.Value as ExtensionObject;

            Assert.NotNull(extObj);

            NodeId encodingId = extObj.TypeId;

            // 2. Register
            CustomUaTypeRegistry.Register(encodingId, S7Dtl.Decode, (val, w) => val.Encode(w));

            // 3. Read again, now with decoding
            var results2 = await client.ReadAsync([dtlNodeId]);
            var extObj2 = results2?[0].Value?.Value as ExtensionObject;

            Assert.NotNull(extObj2?.DecodedValue);
            Assert.IsType<S7Dtl>(extObj2.DecodedValue);

            var dtl = (S7Dtl)extObj2.DecodedValue;

            // Year should be > 2000
            Assert.True(dtl.Year > 2000);
        }

        [Fact]
        public async Task Write_S7DTL()
        {
            NodeId variableNodeId = new(4, 25);
            NodeId typeEncodingId = new(4, 24);

            // 1. Register
            CustomUaTypeRegistry.Register<S7Dtl>(typeEncodingId, S7Dtl.Decode, (val, w) => val.Encode(w));

            // 2. Create object
            var myTime = new S7Dtl
            {
                Year = 2025,
                Month = 12,
                Day = 24,
                Hour = 18,
                Minute = 30,
                Second = 0
            };

            // 3. Wrapper
            var extObj = new ExtensionObject { DecodedValue = myTime };
            var dataValue = new DataValue
            {
                Value = new Variant(extObj, BuiltInType.ExtensionObject)
            };

            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:s7nexus:client", "urn:s7nexus", "WriteDTL");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            // INITIAL READ
            var initialRead = await client.ReadAsync([variableNodeId]);
            var initialExt = initialRead?[0]?.Value?.Value as ExtensionObject;
            Assert.NotNull(initialExt);
            var initialDtl = (S7Dtl?)initialExt.DecodedValue;
            Assert.NotNull(initialDtl);


            // WRITE
            var results = await client.WriteAsync([variableNodeId], [dataValue]);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            Assert.True(results?[0].IsGood);

            // READ BACK
            var readRes = await client.ReadAsync([variableNodeId]);
            var readExt = readRes?[0].Value?.Value as ExtensionObject;
            Assert.NotNull(readExt);
            var readDtl = (S7Dtl?)readExt.DecodedValue;
            Assert.NotNull(readDtl);
            Assert.Equal(2025, readDtl.Year);

            // RESET
            var origDataValue = new DataValue
            {
                Value = new Variant(initialExt, BuiltInType.ExtensionObject)
            };
            results = await client.WriteAsync([variableNodeId], [origDataValue]);
        }

        [Fact]
        public async Task Write_And_Read_Back_Int16()
        {
            var nodeId = new NodeId(4, 72);
            short newValue = 1234;

            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:s7nexus:client", "urn:s7nexus", "WriteSession");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            // 1. Read original value
            var originalRead = (await client.ReadAsync([nodeId]))?.FirstOrDefault()?.Value?.Value;
            Assert.NotNull(originalRead);

            // 2. Prepare new DataValue
            var dataValue = new DataValue
            {
                Value = new Variant(newValue, BuiltInType.Int16)
            };

            // 3. Write
            var results = await client.WriteAsync([nodeId], [dataValue]);

            Assert.NotNull(results);
            Assert.Single(results);
            Assert.True(results[0].IsGood);

            // 4. Read back
            var readResults = await client.ReadAsync([nodeId]);

            var readVal = (short?)readResults?[0].Value?.Value;
            Assert.NotNull(readVal);
            Assert.Equal(newValue, readVal);

            // 5. Reset to original
            dataValue = new DataValue
            {
                Value = new Variant(originalRead, BuiltInType.Int16)
            };

            results = await client.WriteAsync([nodeId], [dataValue]);

            Assert.NotNull(results);
            Assert.Single(results);
            Assert.True(results[0].IsGood);
        }

        [Fact]
        public async Task Write_Custom_Struct_Array()
        {
            NodeId variableNodeId = new(4, 105);

            var myStructs = new TestStruct[3];
            myStructs[0] = new TestStruct { TestStructBool = true, TestStructInt = 10, TestStructString = "Elem 1" };
            myStructs[1] = new TestStruct { TestStructBool = false, TestStructInt = 20, TestStructString = "Elem 2" };
            myStructs[2] = new TestStruct { TestStructBool = true, TestStructInt = 30, TestStructString = "Elem 3" };

            var extObjArray = new ExtensionObject[3];
            for (int i = 0; i < 3; i++)
            {
                extObjArray[i] = new ExtensionObject { DecodedValue = myStructs[i] };
            }

            var dataValue = new DataValue
            {
                Value = new Variant(extObjArray, BuiltInType.ExtensionObject, true) // manually set IsArray = true
            };

            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:s7nexus:client", "urn:s7nexus", "WriteStructArr");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            // INITIAL READ
            var initialRead = await client.ReadAsync([variableNodeId]);
            Assert.NotNull(initialRead?[0]?.Value);
            Assert.True(initialRead?[0]?.Value?.IsArray);

            var initialExtArray = initialRead?[0]?.Value!.Value as ExtensionObject[];
            Assert.NotNull(initialExtArray);
            Assert.Equal(3, initialExtArray.Length);

            // WRITE
            var results = await client.WriteAsync([variableNodeId], [dataValue]);
            Assert.True(results?[0].IsGood);

            // READ BACK (Verify)
            var readRes = await client.ReadAsync([variableNodeId]);
            Assert.NotNull(readRes?[0]?.Value);
            var readExtArray = readRes[0].Value!.Value as ExtensionObject[];

            Assert.NotNull(readExtArray);
            Assert.Equal(3, readExtArray.Length);

            // Check
            var item0 = (TestStruct?)readExtArray[0].DecodedValue;
            var item1 = (TestStruct?)readExtArray[1].DecodedValue;
            var item2 = (TestStruct?)readExtArray[2].DecodedValue;

            Assert.NotNull(item0);
            Assert.NotNull(item1);
            Assert.NotNull(item2);

            Assert.True(item0.TestStructBool);
            Assert.Equal("Elem 1", item0.TestStructString);

            Assert.False(item1.TestStructBool);
            Assert.Equal(20, item1.TestStructInt);

            Assert.Equal("Elem 3", item2.TestStructString);

            // RESET
            var origDataValue = new DataValue
            {
                Value = new Variant(initialExtArray, BuiltInType.ExtensionObject, true)
            };
            await client.WriteAsync([variableNodeId], [origDataValue]);
        }

        [Fact]
        public async Task Browse_Objects_Folder()
        {
            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:s7nexus:client", "urn:s7nexus", "Browse");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            // Objects Folder: i=85
            var references = await client.BrowseAsync([new NodeId(0, 85)]);

            Assert.NotNull(references);
            Assert.NotEmpty(references);
            Assert.Single(references);

            // Check for "Server" object
            Assert.Contains(references[0], r => r.BrowseName?.Name == "Server");
        }

        [Fact]
        public async Task Subscribe_Value_Change()
        {
            // Setup
            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:sub", "urn:sub", "SubSession");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            var sub = new Subscription(client);
            await sub.CreateAsync(500);

            var tcsInitial = new TaskCompletionSource<short>();
            var tcsUpdate = new TaskCompletionSource<short>();

            // NodeId (TestInt)
            var targetNode = new NodeId(4, 72);

            sub.DataChanged += (handle, val) =>
            {
                if (handle == 42 && val.Value != null)
                {
                    short? currentVal = (short?)val.Value.Value;

                    Assert.NotNull(currentVal);

                    if (!tcsInitial.Task.IsCompleted)
                    {
                        tcsInitial.TrySetResult((short)currentVal);
                    }
                    else if (!tcsUpdate.Task.IsCompleted)
                    {
                        short oldVal = tcsInitial.Task.Result;
                        if (currentVal != oldVal)
                        {
                            tcsUpdate.TrySetResult((short)currentVal);
                        }
                    }
                }
            };

            // 1. Start Subscription
            await sub.CreateMonitoredItemAsync(targetNode, 42);

            // 2. Wait for initial value (max 5s)
            var initialTask = await Task.WhenAny(tcsInitial.Task, Task.Delay(5000));
            if (initialTask != tcsInitial.Task) throw new TimeoutException("Initial value not received");

            short initialValue = await tcsInitial.Task;

            // 3. Trigger: Change value
            short newValue = (short)(initialValue + 1);
            var writeResults = await client.WriteAsync([targetNode], [
                new DataValue { Value = new Variant(newValue, BuiltInType.Int16) }
            ]);

            Assert.True(writeResults?[0].IsGood);

            // 4. Wait for update (max 5s)
            var updateTask = await Task.WhenAny(tcsUpdate.Task, Task.Delay(5000));
            if (updateTask != tcsUpdate.Task) throw new TimeoutException("Updated value not received via Subscription");

            short receivedUpdate = await tcsUpdate.Task;

            // 5. Assert
            Assert.Equal(newValue, receivedUpdate);

            // Cleanup: Reset value
            await client.WriteAsync([targetNode], [
                new DataValue { Value = new Variant(initialValue, BuiltInType.Int16) }
            ]);

            sub.Stop();
        }

        [Fact]
        public async Task Call_Add_Method()
        {
            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:call", "urn:call", "CallSession");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            // object and method NodeIds
            var objectId = new NodeId(4, 158);
            var methodId = new NodeId(4, 159);

            // --- Input Wrapping ---

            var arg1 = new Variant((short)7, BuiltInType.Int16);
            var arg2 = new Variant((short)5, BuiltInType.Int16);

            // Call
            var outputs = await client.CallAsync(objectId, methodId, default, arg1, arg2);

            Assert.NotNull(outputs);
            Assert.Single(outputs);

            // --- Output Unwrapping ---
            var outputVar = outputs[0].Value as int?;
            Assert.NotNull(outputVar);
            Assert.Equal(12, outputVar.Value); // 7 + 5 = 12
        }

        [Fact]
        public async Task Call_Add_Method_Typed()
        {
            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:call", "urn:call", "CallTyped");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            var input = new AddInput { ValA = 10, ValB = 20 };

            // Typed call
            var result = await client.CallTypedAsync<AddInput, AddOutput>(
                new NodeId(4, 158),
                new NodeId(4, 159),
                input);

            Assert.Equal(30, result.Result);
        }

        [Fact]
        public async Task Browse_With_Paging()
        {
            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:test", "urn:test", "BrowseNext");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            var refs = await client.BrowseAsync([new NodeId(0, 2253)], maxRefs: 2);

            Assert.NotNull(refs);
            Assert.NotEmpty(refs);
            Assert.Single(refs);
            Assert.NotEmpty(refs[0]);
            Assert.True(refs[0].Length > 2);
        }

        [Fact]
        public async Task Modify_And_Toggle_Subscription()
        {
            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:test", "urn:test", "LifecycleTest");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            // 1. Create Subscription
            var createSubReq = new CreateSubscriptionRequest
            {
                RequestHeader = client.CreateRequestHeader(),
                RequestedPublishingInterval = 500.0,
                PublishingEnabled = true
            };
            var subRes = await client.SendRequestAsync<CreateSubscriptionRequest, CreateSubscriptionResponse>(createSubReq);
            uint subId = subRes.SubscriptionId;
            Assert.NotEqual((uint)0, subId);

            // 2. Create Monitored Item
            var createMonReq = new CreateMonitoredItemsRequest
            {
                RequestHeader = client.CreateRequestHeader(),
                SubscriptionId = subId,
                ItemsToCreate =
                [
                        new MonitoredItemCreateRequest(

                            new ReadValueId(new NodeId(4, 3)), // TestBool
                            2,
                            new MonitoringParameters() { ClientHandle = 1, SamplingInterval = 100, QueueSize = 1 }
                        )
                    ]
            };
            var monRes = await client.SendRequestAsync<CreateMonitoredItemsRequest, CreateMonitoredItemsResponse>(createMonReq);
            uint monId = monRes.Results?[0].MonitoredItemId ?? 0;
            Assert.NotEqual((uint)0, monId);

            // 3. Modify Monitored Item
            var modResults = await client.ModifyMonitoredItemsAsync(
                subId,
                [monId],
                [(uint)1], // keep handle
                2000.0, // New interval
                5,      // New queue
                default
            );
            Assert.True(modResults?[0].StatusCode.IsGood);

            var setMonResults = await client.SetMonitoringModeAsync(
                subId,
                [monId],
                0, // Disabled
                default
            );
            Assert.True(setMonResults?[0].IsGood);

            // 5. Set Publishing Mode (Helper)
            var setPubResults = await client.SetPublishingModeAsync(
                [subId],
                false, // Disabled
                default
            );
            Assert.True(setPubResults?[0].IsGood);

            // Cleanup
            var delReq = new DeleteSubscriptionsRequest
            {
                RequestHeader = client.CreateRequestHeader(),
                SubscriptionIds = [subId]
            };
            await client.SendRequestAsync<DeleteSubscriptionsRequest, DeleteSubscriptionsResponse>(delReq);
        }

        [Fact]
        public async Task Browse_Multiple_Nodes_At_Once()
        {
            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:test", "urn:test", "BrowseBulk");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            var nodes = new[]
            {
                    new NodeId(0, 84), // Root
                    new NodeId(0, 85)  // Objects
                };

            var results = await client.BrowseAsync(nodes, 2);

            Assert.Equal(2, results.Length);

            Assert.Contains(results[0], r => r.BrowseName?.Name == "Objects");
            Assert.Contains(results[0], r => r.BrowseName?.Name == "Types");
            Assert.Contains(results[0], r => r.BrowseName?.Name == "Views");

            Assert.Contains(results[1], r => r.BrowseName?.Name == "Server");
            Assert.Contains(results[1], r => r.BrowseName?.Name == "DeviceSet");
            Assert.Contains(results[1], r => r.BrowseName?.Name == "PLC_1");
        }

        [Fact]
        public async Task Translate_Path_To_NodeId()
        {
            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();
            await client.CreateSessionAsync("urn:test", "urn:test", "Translate");
            await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

            // Start: Objects (i=85)
            // path to ServerStatus: "0:Server/0:ServerStatus"

            var startNode = new NodeId(0, 85);
            var paths = new[] { "0:Server/0:ServerStatus" };

            var nodeIds = await client.ResolveNodeIdsAsync(startNode, paths);

            Assert.Single(nodeIds);
            Assert.NotNull(nodeIds[0]);

            // should be NodeId i=2256 (ServerStatus)
            Assert.Equal((uint)2256, nodeIds[0]?.NumericIdentifier);
        }

        #region Helper classes
        private class AddInput
        {
            [OpcMethodParameter(0, BuiltInType.Int16)]
            public short ValA { get; set; }

            [OpcMethodParameter(1, BuiltInType.Int16)]
            public short ValB { get; set; }
        }

        private class AddOutput
        {
            [OpcMethodParameter(0, BuiltInType.Int32)]
            public int Result { get; set; }
        }

        private class S7Dtl
        {
            public ushort Year { get; set; }
            public byte Month { get; set; }
            public byte Day { get; set; }
            public byte Weekday { get; set; }
            public byte Hour { get; set; }
            public byte Minute { get; set; }
            public byte Second { get; set; }
            public uint Nanosecond { get; set; }

            public DateTime ToDateTime()
            {
                return new DateTime(Year, Month, Day, Hour, Minute, Second).AddTicks(Nanosecond / 100);
            }

            public static S7Dtl Decode(OpcUaBinaryReader reader)
            {
                var dtl = new S7Dtl
                {
                    Year = reader.ReadUInt16(),
                    Month = reader.ReadByte(),
                    Day = reader.ReadByte(),
                    Weekday = reader.ReadByte(),
                    Hour = reader.ReadByte(),
                    Minute = reader.ReadByte(),
                    Second = reader.ReadByte(),
                    Nanosecond = reader.ReadUInt32()
                };
                return dtl;
            }

            public void Encode(OpcUaBinaryWriter writer)
            {
                writer.WriteUInt16(Year);
                writer.WriteByte(Month);
                writer.WriteByte(Day);
                writer.WriteByte(Weekday);
                writer.WriteByte(Hour);
                writer.WriteByte(Minute);
                writer.WriteByte(Second);
                writer.WriteUInt32(Nanosecond);
            }

            public override string ToString() => $"{Year}-{Month}-{Day} {Hour}:{Minute}:{Second}.{Nanosecond}";
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

            public override string ToString() => $"TestStructBool:{TestStructBool} - TestStructInt:{TestStructInt} - TestStructString:{TestStructString}";
        }
        #endregion
        */
    }
}
