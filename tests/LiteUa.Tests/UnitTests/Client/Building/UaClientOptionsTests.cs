using LiteUa.Client.Building;
using LiteUa.Security.Policies;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session.Identity;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LiteUa.Tests.UnitTests.Client.Building
{
    [Trait("Category", "Unit")]
    public class UaClientOptionsTests
    {
        [Fact]
        public void Constructor_InitializesNestedOptions()
        {
            // Act
            var options = new UaClientOptions();

            // Assert
            Assert.NotNull(options.Security);
            Assert.NotNull(options.Session);
            Assert.NotNull(options.Pool);
            Assert.Equal(string.Empty, options.EndpointUrl);
        }

        [Fact]
        public void SecurityOptions_DefaultValues_AreCorrect()
        {
            // Act
            var security = new UaClientOptions.SecurityOptions();

            // Assert
            Assert.False(security.AutoAcceptUntrustedCertificates);
            Assert.Equal(MessageSecurityMode.None, security.MessageSecurityMode);
            Assert.Equal(SecurityPolicyType.None, security.PolicyType);
            Assert.Equal(UserTokenType.Anonymous, security.UserTokenType);
            Assert.Null(security.Username);
            Assert.Null(security.Password);
            Assert.Null(security.ClientCertificate);
        }

        [Fact]
        public void SessionOptions_DefaultValues_AreCorrect()
        {
            // Act
            var session = new UaClientOptions.SessionOptions();

            // Assert
            Assert.Equal("LiteUa Client", session.ApplicationName);
            Assert.Equal("urn:LiteUa:client", session.ApplicationUri);
            Assert.Equal("urn:github.com/philipp2604/LiteUa", session.ProductUri);
        }

        [Fact]
        public void PoolOptions_DefaultValues_AreCorrect()
        {
            // Act
            var pool = new UaClientOptions.PoolOptions();

            // Assert
            Assert.Equal(10, pool.MaxSize);
        }

        [Fact]
        public void Properties_CanBeUpdated()
        {
            // Arrange
            var options = new UaClientOptions();
            var testUrl = "opc.tcp://10.0.0.1:4840";
            var testUser = "Admin";

            // Act
            options.EndpointUrl = testUrl;
            options.Security.Username = testUser;
            options.Security.MessageSecurityMode = MessageSecurityMode.SignAndEncrypt;
            options.Pool.MaxSize = 50;

            // Assert
            Assert.Equal(testUrl, options.EndpointUrl);
            Assert.Equal(testUser, options.Security.Username);
            Assert.Equal(MessageSecurityMode.SignAndEncrypt, options.Security.MessageSecurityMode);
            Assert.Equal(50, options.Pool.MaxSize);
        }

        [Fact]
        public void SecurityOptions_CanStoreCertificate()
        {
            // Arrange
            var security = new UaClientOptions.SecurityOptions();
#pragma warning disable SYSLIB0026
            var cert = new X509Certificate2();
#pragma warning restore SYSLIB0026

            // Act
            security.ClientCertificate = cert;

            // Assert
            Assert.NotNull(security.ClientCertificate);
            Assert.Same(cert, security.ClientCertificate);
        }
    }
}
