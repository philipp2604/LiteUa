using LiteUa.BuiltIn;
using LiteUa.Client;
using LiteUa.Encoding;
using LiteUa.Security;
using LiteUa.Security.Policies;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.Method;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Stack.Subscription;
using LiteUa.Transport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit.Abstractions;

namespace LiteUa.Tests.IntegrationTests
{
    [Category("IntegrationTests_S7-1500")]
    public class UaTcpClientChannelIntegrationTests_S7_1500
    {
        private readonly ITestOutputHelper _output;
        public const string TestServerUrl = "opc.tcp://192.178.0.1:4840/";

        public UaTcpClientChannelIntegrationTests_S7_1500(ITestOutputHelper output)
        {
            _output = output;
            RegisterCustomTypes();
        }

        ~UaTcpClientChannelIntegrationTests_S7_1500()
        {
            CustomUaTypeRegistry.Clear();
        }

        private static void RegisterCustomTypes()
        {
            CustomUaTypeRegistry.Register<TestStruct>(new NodeId(3, "TE_\"Variables\".\"TestStructArray\""), TestStruct.Decode, (val, w) => val.Encode(w));
            CustomUaTypeRegistry.Register<DT_Object>(new NodeId(3, "TE_\"DT_Object\""), DT_Object.Decode, (val, w) => val.Encode(w));
        }

        [Fact]
        public async Task Connect_To_Server()
        {
            // 1. Arrange
            await using var uaClient = new UaTcpClientChannel(TestServerUrl);

            // 2. Act
            await uaClient.ConnectAsync();

            // 3. Assert
            Assert.True(uaClient.ReceiveBufferSize > 0);
            Assert.True(uaClient.SendBufferSize > 0);
        }

        [Fact]
        public async Task GetEndpoints_From_Server()
        {
            // 1. Arrange
            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync();

            // 2. Act
            var endpointsResponse = await client.GetEndpointsAsync();

            // 3. Assert
            Assert.NotNull(endpointsResponse);
            Assert.NotNull(endpointsResponse.Endpoints);
            Assert.NotEmpty(endpointsResponse.Endpoints);
        }

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
            var testConfigs = new (string Name, NodeId NodeId, BuiltInType Type, bool IsArray)[]
            {
                ("TestBool",        new(3, "\"Variables\".\"TestBool\""), BuiltInType.Boolean, false),
                ("TestBoolArray",   new(3, "\"Variables\".\"TestBoolArray\""), BuiltInType.Boolean, true),
                ("TestByte",        new(3, "\"Variables\".\"TestByte\""), BuiltInType.Byte,    false),
                ("TestByteArray",   new(3, "\"Variables\".\"TestByteArray\""), BuiltInType.Byte,    true),
                ("TestChar",        new(3, "\"Variables\".\"TestChar\""), BuiltInType.Byte,    false),
                ("TestCharArray",   new(3, "\"Variables\".\"TestCharArray\""), BuiltInType.Byte,    true),
                ("TestDInt",        new(3, "\"Variables\".\"TestDInt\""), BuiltInType.Int32,   false),
                ("TestDIntArray",   new(3, "\"Variables\".\"TestDIntArray\""), BuiltInType.Int32,   true),
                ("TestDTL",         new(3, "\"Variables\".\"TestDtl\""), BuiltInType.ExtensionObject, false),
                ("TestDTLArray",    new(3, "\"Variables\".\"TestDtlArray\""), BuiltInType.ExtensionObject, true),
                ("TestDWord",       new(3, "\"Variables\".\"TestDWord\""), BuiltInType.UInt32,  false),
                ("TestDWordArray",  new(3, "\"Variables\".\"TestDWordArray\""), BuiltInType.UInt32,  true),
                ("TestDate",        new(3, "\"Variables\".\"TestDate\""), BuiltInType.UInt16,  false),
                ("TestDateArray",   new(3, "\"Variables\".\"TestDateArray\""), BuiltInType.UInt16,  true),
                ("TestInt",         new(3, "\"Variables\".\"TestInt\""), BuiltInType.Int16,   false),
                ("TestIntArray",    new(3, "\"Variables\".\"TestIntArray\""), BuiltInType.Int16,   true),
                ("TestLReal",       new(3, "\"Variables\".\"TestLReal\""), BuiltInType.Double,  false),
                ("TestLRealArray",  new(3, "\"Variables\".\"TestLRealArray\""), BuiltInType.Double,  true),
                ("TestReal",        new(3, "\"Variables\".\"TestReal\""), BuiltInType.Float,   false),
                ("TestRealArray",   new(3, "\"Variables\".\"TestRealArray\""), BuiltInType.Float,   true),
                ("TestSInt",        new(3, "\"Variables\".\"TestSInt\""), BuiltInType.SByte,   false),
                ("TestSIntArray",   new(3, "\"Variables\".\"TestSIntArray\""), BuiltInType.SByte,   true),
                ("TestString",      new(3, "\"Variables\".\"TestString\""), BuiltInType.String,  false),
                ("TestStringArray", new(3, "\"Variables\".\"TestStringArray\""), BuiltInType.String,  true),
                ("TestStruct",      new(3, "\"Variables\".\"TestStruct\""), BuiltInType.ExtensionObject, false),
                ("TestStructArray", new(3, "\"Variables\".\"TestStructArray\""), BuiltInType.ExtensionObject, true),
                ("TestTime",        new(3, "\"Variables\".\"TestTime\""), BuiltInType.Int32,   false),
                ("TestTimeArray",   new(3, "\"Variables\".\"TestTimeArray\""), BuiltInType.Int32,   true),
                ("TestTimeOfDay",   new(3, "\"Variables\".\"TestTimeOfDay\""), BuiltInType.UInt32,  false),
                ("TestTimeOfDayArray", new(3, "\"Variables\".\"TestTimeOfDayArray\""), BuiltInType.UInt32, true),
                ("TestUDInt",       new(3, "\"Variables\".\"TestUDInt\""), BuiltInType.UInt32,  false),
                ("TestUDIntArray",  new(3, "\"Variables\".\"TestUDIntArray\""), BuiltInType.UInt32,  true),
                ("TestUInt",        new(3, "\"Variables\".\"TestUInt\""), BuiltInType.UInt16,  false),
                ("TestUIntArray",   new(3, "\"Variables\".\"TestUIntArray\""), BuiltInType.UInt16,  true),
                ("TestUSInt",       new(3, "\"Variables\".\"TestUSInt\""), BuiltInType.Byte,    false),
                ("TestUSIntArray",  new(3, "\"Variables\".\"TestUSIntArray\""), BuiltInType.Byte,    true),
                ("TestWChar",       new(3, "\"Variables\".\"TestWChar\""), BuiltInType.UInt16,  false),
                ("TestWCharArray",  new(3, "\"Variables\".\"TestWCharArray\""), BuiltInType.UInt16,  true),
                ("TestWString",     new(3, "\"Variables\".\"TestWString\""), BuiltInType.String,  false),
                ("TestWStringArray",new(3, "\"Variables\".\"TestWStringArray\""), BuiltInType.String,  true),
                ("TestWord",        new(3, "\"Variables\".\"TestWord\""), BuiltInType.UInt16,  false),
                ("TestWordArray",   new(3, "\"Variables\".\"TestWordArray\""), BuiltInType.UInt16,  true),
            };

            var nodesToRead = new NodeId[testConfigs.Length];
            for (int i = 0; i < testConfigs.Length; i++)
            {
                nodesToRead[i] = testConfigs[i].NodeId;
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
            NodeId dtlNodeId = new(3, "\"Variables\".\"TestDtl\"");
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
            NodeId variableNodeId = new(3, "\"Variables\".\"TestDtl\"");
            NodeId typeEncodingId = new(3, "TE_DTL");

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
            var nodeId = new NodeId(3, "\"Variables\".\"TestInt\"");
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
            NodeId variableNodeId = new(3, "\"Variables\".\"TestStructArray\"");

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
            var references = await client.BrowseAsync(new NodeId(0, 85));

            Assert.NotNull(references);
            Assert.NotEmpty(references);

            // Check for "Server" object
            Assert.Contains(references, r => r.BrowseName?.Name == "Server");
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
            var targetNode = new NodeId(3, "\"Variables\".\"TestInt\"");

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
            var objectId = new NodeId(3, "\"ADD_Method_DB\"");
            var methodId = new NodeId(3, "\"ADD_Method_DB\".Method");

            // --- Input Wrapping ---

            var arg1 = new Variant((short)7, BuiltInType.Int16);
            var arg2 = new Variant((short)5, BuiltInType.Int16);

            // Call mit params Array
            var outputs = await client.CallAsync(objectId, methodId, arg1, arg2);

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
                new NodeId(3, "\"ADD_Method_DB\""),
                new NodeId(3, "\"ADD_Method_DB\".Method"),
                input);

            Assert.Equal(30, result.Result);
        }

        [Fact]
        public async Task Call_Method_With_Nested_Struct_Array()
        {
            var objectId = new NodeId(3, "\"COMPLEX_Method_DB\""); // Das SPS-Objekt, auf dem die Methode liegt
            var methodId = new NodeId(3, "\"COMPLEX_Method_DB\".Method"); // Die Methode selbst

            var encodingIdDtObject = new NodeId(0, 296);

            await using (var client = new UaTcpClientChannel(TestServerUrl))
            {
                await client.ConnectAsync();
                await client.CreateSessionAsync("urn:s7nexus:client", "urn:s7nexus", "MethodSession");
                await client.ActivateSessionAsync(new AnonymousIdentity("Anonymous"));

                var inputData = new ComplexInput
                {
                    InputObjects = new DT_Object[3]
                };

                inputData.InputObjects[0] = new DT_Object("Test", new DT_Pos { X = 10, Y = 20, Z = 30 });
                inputData.InputObjects[1] = new DT_Object("Elem 2", new DT_Pos { X = 11, Y = 21, Z = 31});
                inputData.InputObjects[2] = new DT_Object("Z Object", new DT_Pos { X = 12, Y = 22, Z = 32 });

                var result = await client.CallTypedAsync<ComplexInput, ComplexOutput>(
                    objectId,
                    methodId,
                    inputData);

                // 4. Prüfen
                Assert.NotNull(result);
                Assert.NotNull(result.OutputObjects);
                Assert.Equal(3, result.OutputObjects.Length);

                var out1 = result.OutputObjects[0];
                var out3 = result.OutputObjects[2];

                Assert.Equal("Test", out1.Name);
                Assert.Equal(10, out1.Position.X);
                Assert.Equal("Z Object", out3.Name);
                Assert.Equal(12, out3.Position.X);
            }
        }

        #region Helper classes

        class DT_Pos
        {
            public short X { get; set; }
            public short Y { get; set; }
            public short Z { get; set; }

            public static DT_Pos Decode(OpcUaBinaryReader reader)
            {
                var pos = new DT_Pos
                {
                    X = reader.ReadInt16(),
                    Y = reader.ReadInt16(),
                    Z = reader.ReadInt16()
                };

                return pos;
            }

            public void Encode(OpcUaBinaryWriter writer)
            {
                writer.WriteInt16(X);
                writer.WriteInt16(Y);
                writer.WriteInt16(Z);
            }
        }

        class DT_Object
        {
            public string Name { get; set; }
            public DT_Pos Position { get; set; }

            public DT_Object(string name,  DT_Pos position)
            {
                Name = name;
                Position = position;
            }

            public static DT_Object Decode(OpcUaBinaryReader reader)
            {
                var ob = new DT_Object(reader.ReadString() ?? string.Empty, DT_Pos.Decode(reader));
                return ob;
            }

            public void Encode(OpcUaBinaryWriter writer)
            {
                writer.WriteString(Name);
                Position.Encode(writer);
            }
        }

        private class ComplexInput
        {
            [OpcMethodParameter(0, BuiltInType.ExtensionObject)]
            public DT_Object[]? InputObjects { get; set; }
        }

        class ComplexOutput
        {
            [OpcMethodParameter(0, BuiltInType.ExtensionObject)]
            public DT_Object[]? OutputObjects { get; set; }
        }

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

        private class TestStructArray
        {
            public TestStruct[] Members { get; set; } = new TestStruct[3];

            public static TestStructArray Decode(OpcUaBinaryReader reader)
            {
                var length = reader.ReadInt32();
                var members = new TestStruct[length];
                for (int i = 0; i < length; i++)
                {
                    members[i] = TestStruct.Decode(reader);
                }

                return new TestStructArray() { Members = members };
            }

            public void Encode(OpcUaBinaryWriter writer)
            {
                writer.WriteInt32(Members.Length);
                for(int i = 0; i < Members.Length; i++)
                {
                    Members[i].Encode(writer);
                }
            }
        }
        #endregion
    }
}
