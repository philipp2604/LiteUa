using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Security;
using LiteUa.Security.Policies;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using LiteUa.Transport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit.Abstractions;

namespace LiteUa.Tests.IntegrationTests
{
    [Category("IntegrationTests_S7-1200")]
    public class UaTcpClientChannelIntegrationTests_S7_1200
    {
        private ITestOutputHelper _output;
        public const string TestServerUrl = "opc.tcp://192.178.0.1:4840/";

        public UaTcpClientChannelIntegrationTests_S7_1200(ITestOutputHelper output)
        {
            _output = output;
            RegisterCustomTypes();
        }

        private void RegisterCustomTypes()
        {
            //TestStruct: 4,98

        }

        [Fact]
        public async Task Connect_To_Server()
        {
            // 1. Arrange
            await using var uaClient = new UaTcpClientChannel(TestServerUrl);

            // 2. Act
            await uaClient.ConnectAsync(default);

            // 3. Assert
            Assert.True(uaClient.ReceiveBufferSize > 0);
            Assert.True(uaClient.SendBufferSize > 0);
        }

        [Fact]
        public async Task GetEndpoints_From_Server()
        {
            // 1. Arrange
            await using var client = new UaTcpClientChannel(TestServerUrl);
            await client.ConnectAsync(default);

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

            Console.WriteLine("1. Discovery...");
            await using (var discoveryClient = new UaTcpClientChannel(TestServerUrl))
            {
                await discoveryClient.ConnectAsync(default);
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
            Console.WriteLine("2. Generating Certificates...");

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
            await secureClient.ConnectAsync(default);
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
                await discovery.ConnectAsync(default);
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
            await using (var client = new UaTcpClientChannel(TestServerUrl, policy, clientCert, serverCert, MessageSecurityMode.SignAndEncrypt))
            {
                await client.ConnectAsync(default);

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
        }

        [Fact]
        public async Task Login_With_Username_And_Password()
        {
            // 1. Discovery
            EndpointDescription? targetEndpoint = null;
            UserTokenPolicy? userTokenPolicy = null;

            Console.WriteLine("1. Discovery...");
            await using (var discovery = new UaTcpClientChannel(TestServerUrl))
            {
                await discovery.ConnectAsync(default);
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

            Console.WriteLine("2. Connecting (Plain Text Channel)...");
            await using (var client = new UaTcpClientChannel(
                TestServerUrl,
                channelPolicy,
                clientCert,
                serverCert,
                MessageSecurityMode.SignAndEncrypt))
            {
                await client.ConnectAsync(default);

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
        }
    }
}
