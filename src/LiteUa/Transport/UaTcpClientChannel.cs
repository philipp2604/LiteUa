using LiteUa.BuiltIn;
using LiteUa.Client.Events;
using LiteUa.Encoding;
using LiteUa.Security;
using LiteUa.Security.Policies;
using LiteUa.Stack.Attribute;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.Method;
using LiteUa.Stack.SecureChannel;
using LiteUa.Stack.Session;
using LiteUa.Stack.Session.Identity;
using LiteUa.Stack.Subscription;
using LiteUa.Stack.Subscription.MonitoredItem;
using LiteUa.Stack.View;
using LiteUa.Transport.Headers;
using LiteUa.Transport.TcpMessages;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Transport
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class UaTcpClientChannel : IUaTcpClientChannel, IDisposable, IAsyncDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly string _endpointUrl;
        private readonly string _applicationUri;
        private readonly string _productUri;
        private readonly string _applicationName;

        private readonly SemaphoreSlim _lock = new(1, 1);

        // --- Connection State ---
        private uint _sequenceNumber = 1;

        private int _requestId = 1;
        public uint SecureChannelId = 0; // 0 while disconnected
        private NodeId _authenticationToken = new(0); // Session Token
        private uint _tokenId;

        // --- Renewal Management ---
        private CancellationTokenSource? _renewCts;

        private Task? _renewTask;

        // --- Configuration / Security ---
        private readonly ISecurityPolicy _securityPolicy;

        private readonly X509Certificate2? _clientCertificate;
        private X509Certificate2? _serverCertificate;
        private byte[]? _clientNonce;
        private readonly MessageSecurityMode _securityMode;
        private byte[]? _sessionServerNonce;
        private byte[]? _sessionClientNonce;

        public event EventHandler<CertificateValidationEventArgs>? CertificateValidation;

        // --- Negotiated Parameters ---
        public uint SendBufferSize { get; private set; }

        public uint ReceiveBufferSize { get; private set; }
        public uint MaxMessageSize { get; private set; }
        public uint MaxChunkCount { get; private set; }

        public UaTcpClientChannel(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            ISecurityPolicyFactory policyFactory,
            MessageSecurityMode securityMode,
            X509Certificate2? clientCertificate,
            X509Certificate2? serverCertificate)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpointUrl);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(productUri);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(applicationName);
            ArgumentNullException.ThrowIfNull(policyFactory);

            _endpointUrl = endpointUrl;
            _applicationUri = applicationUri;
            _productUri = productUri;
            _applicationName = applicationName;
            _securityPolicy = policyFactory.CreateSecurityPolicy(clientCertificate, serverCertificate);
            _securityMode = securityMode;
            _clientCertificate = clientCertificate;
            _serverCertificate = serverCertificate;

            // Generate nonce (min 32 bytes for 256bit security)
            _clientNonce = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(_clientNonce);
        }

        public async Task<NodeId?[]> ResolveNodeIdsAsync(NodeId startNode, string[] paths, CancellationToken token = default)
        {
            var browsePaths = new BrowsePath[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                browsePaths[i] = new BrowsePath(startNode, BrowsePathParser.Parse(paths[i]));
            }

            var req = new TranslateBrowsePathsToNodeIdsRequest
            {
                RequestHeader = CreateRequestHeader(),
                BrowsePaths = browsePaths
            };

            var res = await SendRequestAsync<TranslateBrowsePathsToNodeIdsRequest, TranslateBrowsePathsToNodeIdsResponse>(req, token);

            var results = new NodeId?[paths.Length];
            for (int i = 0; i < res.Results?.Length; i++)
            {
                var r = res.Results[i];
                if (r.StatusCode.IsGood && r.Targets != null && r.Targets.Length > 0)
                {
                    // take the first one
                    results[i] = r.Targets[0].TargetId?.NodeId;
                }
                else
                {
                    results[i] = null; // not found
                }
            }
            return results;
        }

        public async Task<MonitoredItemModifyResult[]?> ModifyMonitoredItemsAsync(
            uint subscriptionId,
            uint[] monitoredItemIds,
            uint[] clientHandles,
            double samplingInterval,
            uint queueSize,
            CancellationToken token = default)
        {
            if (monitoredItemIds.Length != clientHandles.Length)
                throw new ArgumentException("Ids and Handles count mismatch");

            var itemsToModify = new MonitoredItemModifyRequest[monitoredItemIds.Length];
            for (int i = 0; i < monitoredItemIds.Length; i++)
            {
                itemsToModify[i] = new MonitoredItemModifyRequest(monitoredItemIds[i],
                    new MonitoringParameters()
                    {
                        ClientHandle = clientHandles[i],
                        SamplingInterval = samplingInterval,
                        QueueSize = queueSize,
                        DiscardOldest = true
                    });
            }

            var req = new ModifyMonitoredItemsRequest
            {
                RequestHeader = CreateRequestHeader(),
                SubscriptionId = subscriptionId,
                TimestampsToReturn = 2, // Both
                ItemsToModify = itemsToModify
            };

            var response = await SendRequestAsync<ModifyMonitoredItemsRequest, ModifyMonitoredItemsResponse>(req, token);
            return response.Results;
        }

        public async Task<ReferenceDescription[][]> BrowseAsync(NodeId[] nodesToBrowse, uint maxRefs = 0, CancellationToken token = default)
        {
            // 1. Initial request
            var browseDescs = new BrowseDescription[nodesToBrowse.Length];
            for (int i = 0; i < nodesToBrowse.Length; i++)
            {
                browseDescs[i] = new BrowseDescription(nodesToBrowse[i])
                {
                    BrowseDirection = BrowseDirection.Forward,
                    IncludeSubtypes = true,
                    NodeClassMask = 0,
                    ResultMask = 63
                };
            }

            var req = new BrowseRequest
            {
                RequestHeader = CreateRequestHeader(),
                NodesToBrowse = browseDescs,
                RequestedMaxReferencesPerNode = maxRefs
            };

            var response = await SendRequestAsync<BrowseRequest, BrowseResponse>(req, token);

            // prepare result: one list per input node
            var allReferences = new List<ReferenceDescription>[nodesToBrowse.Length];
            for (int i = 0; i < allReferences.Length; i++) allReferences[i] = [];

            // list of continuation points
            var pendingContinuationPoints = new List<(int OriginalIndex, byte[] Point)>();

            // first round
            for (int i = 0; i < response.Results?.Length; i++)
            {
                var result = response.Results[i];
                if (result.References != null)
                {
                    allReferences[i].AddRange(result.References);
                }

                if (result.ContinuationPoint != null && result.ContinuationPoint.Length > 0)
                {
                    pendingContinuationPoints.Add((i, result.ContinuationPoint));
                }
            }

            // 2. Paging Loop (BrowseNext)
            while (pendingContinuationPoints.Count > 0)
            {
                var nextReq = new BrowseNextRequest
                {
                    RequestHeader = CreateRequestHeader(),
                    ReleaseContinuationPoints = false,
                    ContinuationPoints = [.. pendingContinuationPoints.Select(x => x.Point)]
                };

                var currentBatchIndices = pendingContinuationPoints.Select(x => x.OriginalIndex).ToList();
                pendingContinuationPoints.Clear();

                var nextResp = await SendRequestAsync<BrowseNextRequest, BrowseNextResponse>(nextReq, token);

                for (int i = 0; i < nextResp?.Results?.Length; i++)
                {
                    var result = nextResp?.Results[i];
                    int originalIndex = currentBatchIndices[i];

                    if (result?.References != null)
                    {
                        allReferences[originalIndex].AddRange(result.References);
                    }

                    if (result?.ContinuationPoint != null && result.ContinuationPoint.Length > 0)
                    {
                        pendingContinuationPoints.Add((originalIndex, result.ContinuationPoint));
                    }
                }
            }

            var finalResult = new ReferenceDescription[nodesToBrowse.Length][];
            for (int i = 0; i < allReferences.Length; i++)
            {
                finalResult[i] = [.. allReferences[i]];
            }

            return finalResult;
        }

        public async Task<StatusCode[]?> SetMonitoringModeAsync(
            uint subscriptionId,
            uint[] monitoredItemIds,
            uint monitoringMode,
            CancellationToken token = default)
        {
            var req = new SetMonitoringModeRequest
            {
                RequestHeader = CreateRequestHeader(),
                SubscriptionId = subscriptionId,
                MonitoringMode = monitoringMode,
                MonitoredItemIds = monitoredItemIds
            };

            var response = await SendRequestAsync<SetMonitoringModeRequest, SetMonitoringModeResponse>(req, token);
            return response.Results;
        }

        public async Task<StatusCode[]?> SetPublishingModeAsync(
            uint[] subscriptionIds,
            bool publishingEnabled,
            CancellationToken token = default)
        {
            var req = new SetPublishingModeRequest
            {
                RequestHeader = CreateRequestHeader(),
                SubscriptionIds = subscriptionIds,
                PublishingEnabled = publishingEnabled
            };

            var response = await SendRequestAsync<SetPublishingModeRequest, SetPublishingModeResponse>(req, token);
            return response.Results;
        }

        public async Task DisconnectAsync()
        {
            _renewCts?.Cancel();

            // close session if exists
            if (_authenticationToken.NumericIdentifier != 0)
            {
                try
                {
                    if (_stream != null)
                    {
                        var req = new CloseSessionRequest
                        {
                            RequestHeader = CreateRequestHeader(),
                            DeleteSubscriptions = true
                        };
                        await SendRequestAsync<CloseSessionRequest, CloseSessionResponse>(req);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            // close secure channel if exists
            if (SecureChannelId != 0)
            {
                try
                {
                    var req = new CloseSecureChannelRequest
                    {
                        RequestHeader = CreateRequestHeader()
                    };
                    await SendRequestAsync<CloseSecureChannelRequest, CloseSecureChannelResponse>(req);
                }
                catch (Exception)
                {
                }
            }

            // close socket
            _stream?.Dispose();
            _client?.Dispose();
            _stream = null;
            _client = null;
        }

        public RequestHeader CreateRequestHeader()
        {
            uint nextHandle = (uint)Interlocked.Increment(ref _requestId);

            return new RequestHeader
            {
                AuthenticationToken = _authenticationToken,
                Timestamp = DateTime.UtcNow,
                RequestHandle = nextHandle,
                TimeoutHint = 10000
            };
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            var uri = new Uri(_endpointUrl);
            /// TODO: throw if port invalid
            int port = uri.Port == -1 ? 4840 : uri.Port;

            _client = new TcpClient();
            await _client.ConnectAsync(uri.Host, port);
            _stream = _client.GetStream();

            // 1. Hello / Acknowledge
            await PerformHandshakeAsync(uri.AbsoluteUri, cancellationToken);

            // 2. Open Secure Channel
            await OpenSecureChannelAsync(cancellationToken);
        }

        public async Task CreateSessionAsync(string sessionName, CancellationToken cancellationToken = default)
        {
            // generate session client nonce
            _sessionClientNonce = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(_sessionClientNonce);

            byte[] clientCertBytes = _clientCertificate?.RawData ?? [];

            var req = new CreateSessionRequest
            {
                RequestHeader = CreateRequestHeader(),
                ClientDescription = new ClientDescription
                {
                    ApplicationUri = _applicationUri,
                    ProductUri = _productUri,
                    ApplicationName = new LocalizedText { Text = _applicationName },
                    Type = ApplicationType.Client,
                    DiscoveryUrls = []
                },

                ServerUri = null,
                EndpointUrl = _endpointUrl,
                SessionName = sessionName,
                ClientNonce = _sessionClientNonce,
                ClientCertificate = clientCertBytes,
                RequestedSessionTimeout = 60000,
                MaxResponseMessageSize = 0
            };

            var response = await SendRequestAsync<CreateSessionRequest, CreateSessionResponse>(req, cancellationToken);

            _authenticationToken = response.AuthenticationToken ?? new NodeId(0, 0);
            _sessionServerNonce = response.ServerNonce;

            if (response.ServerCertificate != null && response.ServerCertificate.Length > 0)
            {
                _serverCertificate = X509CertificateLoader.LoadCertificate(response.ServerCertificate);
            }

            ValidateServerSessionSignature(response, _sessionClientNonce);
        }

        public async Task ActivateSessionAsync(IUserIdentity identity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(identity);

            ExtensionObject userTokenExt = identity.ToExtensionObject(_serverCertificate, _sessionServerNonce);

            var clientSignature = new SignatureData();

            if (_securityMode != MessageSecurityMode.None && _clientCertificate != null)
            {
                /// TODO: support other signature algorithms based on security policy
                clientSignature.Algorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

                if (_sessionServerNonce == null || _sessionServerNonce.Length == 0)
                    throw new InvalidOperationException("Cannot sign ActivateSession: No ServerNonce available.");

                byte[] serverCertBytes = _serverCertificate?.RawData ?? [];
                byte[] dataToSign = new byte[serverCertBytes.Length + _sessionServerNonce.Length];

                Array.Copy(serverCertBytes, 0, dataToSign, 0, serverCertBytes.Length);
                Array.Copy(_sessionServerNonce, 0, dataToSign, serverCertBytes.Length, _sessionServerNonce.Length);

                clientSignature.Signature = _securityPolicy.Sign(dataToSign);
            }
            else
            {
                clientSignature.Algorithm = null;
                clientSignature.Signature = null;
            }

            var req = new ActivateSessionRequest
            {
                RequestHeader = CreateRequestHeader(),
                ClientSignature = clientSignature,
                LocaleIds = ["en-US"],
                UserIdentityToken = userTokenExt,
                UserTokenSignature = new SignatureData()
            };

            var response = await SendRequestAsync<ActivateSessionRequest, ActivateSessionResponse>(req, cancellationToken);

            if (response.ServerNonce != null && response.ServerNonce.Length > 0)
                _sessionServerNonce = response.ServerNonce;
        }

        public async Task<DataValue[]?> ReadAsync(NodeId[] nodesToRead, CancellationToken cancellationToken = default)
        {
            // build request
            var readValues = new ReadValueId[nodesToRead.Length];
            for (int i = 0; i < nodesToRead.Length; i++)
            {
                readValues[i] = new ReadValueId(nodesToRead[i])
                {
                    AttributeId = 13 // Value
                };
            }

            var req = new ReadRequest
            {
                RequestHeader = CreateRequestHeader(),
                NodesToRead = readValues,
                TimestampsToReturn = TimestampsToReturn.Both,
                MaxAge = 0
            };

            var response = await SendRequestAsync<ReadRequest, ReadResponse>(req, cancellationToken);

            return response.Results;
        }

        public async Task<StatusCode[]?> WriteAsync(NodeId[] nodes, DataValue[] values, CancellationToken cancellationToken = default)
        {
            if (nodes.Length != values.Length) throw new ArgumentException("Nodes and Values count mismatch");

            var writeValues = new WriteValue[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                writeValues[i] = new WriteValue(nodes[i], values[i])
                {
                    AttributeId = 13, // Value
                };
            }

            var req = new WriteRequest
            {
                RequestHeader = CreateRequestHeader(),
                NodesToWrite = writeValues
            };

            var response = await SendRequestAsync<WriteRequest, WriteResponse>(req, cancellationToken);
            return response.Results;
        }

        public async Task<Variant[]> CallAsync(NodeId objectId, NodeId methodId, CancellationToken cancellationToken = default, params Variant[] inputArguments)
        {
            var req = new CallRequest(
                [
                    new CallMethodRequest(objectId, methodId, inputArguments)
                ])
            {
                RequestHeader = CreateRequestHeader()
            };

            var response = await SendRequestAsync<CallRequest, CallResponse>(req, cancellationToken);

            if (response.Results == null || response.Results.Length == 0)
                throw new Exception("Empty Call Result");

            var result = response.Results[0];

            if (result is CallMethodResponse res)
            {
                if (!res.StatusCode.IsGood)
                {
                    throw new Exception($"Method Call Failed: {res.StatusCode}");
                }
                else
                {
                    return res.OutputArguments ?? [];
                }
            }
            else
            {
                throw new Exception($"Method Call Failed.");
            }
        }

        private async Task RenewSecureChannelAsync(CancellationToken cancellationToken)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected to server.");

            await _lock.WaitAsync(cancellationToken);
            try
            {
                _clientNonce = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                    rng.GetBytes(_clientNonce);

                var request = new OpenSecureChannelRequest
                {
                    RequestHeader = CreateRequestHeader(),
                    ClientProtocolVersion = 0,
                    RequestType = SecurityTokenRequestType.Renew,
                    SecurityMode = _securityMode,
                    ClientNonce = _clientNonce,
                    RequestedLifetime = 3600000
                };

                // 1. Body Encoding
                byte[] bodyBytes;
                using (var msBody = new MemoryStream())
                {
                    var writerBody = new OpcUaBinaryWriter(msBody);
                    request.Encode(writerBody);
                    bodyBytes = msBody.ToArray();
                }

                // 2. Security Header (Asymmetric)
                byte[]? senderCertData = null;
                byte[]? receiverThumbprint = null;
                if (_securityMode != MessageSecurityMode.None)
                {
                    senderCertData = _clientCertificate?.RawData;
                    receiverThumbprint = _serverCertificate?.GetCertHash();
                }

                var securityHeader = new AsymmetricAlgorithmSecurityHeader
                {
                    SecurityPolicyUri = _securityPolicy.SecurityPolicyUri,
                    SenderCertificate = senderCertData,
                    ReceiverCertificateThumbprint = receiverThumbprint
                };

                byte[] securityHeaderBytes;
                using (var msh = new MemoryStream())
                {
                    var wh = new OpcUaBinaryWriter(msh);
                    securityHeader.Encode(wh);
                    securityHeaderBytes = msh.ToArray();
                }

                // 3. Sequence Header
                var sequenceHeader = new SequenceHeader { SequenceNumber = _sequenceNumber++, RequestId = (uint)_requestId };

                // 4. Padding Calculation
                int plainContentSize = 8 + bodyBytes.Length;
                int signatureSize = _securityPolicy.AsymmetricSignatureSize;
                int inputBlockSize = _securityPolicy.AsymmetricEncryptionBlockSize;
                int outputBlockSize = _securityPolicy.AsymmetricCipherTextBlockSize;

                int paddingSize = PaddingCalculator.CalculatePaddingSize(plainContentSize, signatureSize, inputBlockSize);
                int totalPlainTextSize = plainContentSize + paddingSize + signatureSize;

                int chunks = totalPlainTextSize / inputBlockSize;
                int encryptedDataSize = chunks * outputBlockSize;
                int finalMessageSize = 12 + securityHeaderBytes.Length + encryptedDataSize;

                // 5. Buffer Construction & Encryption
                byte[] plainTextBlob = new byte[totalPlainTextSize];
                using (var msPlain = new MemoryStream(plainTextBlob))
                {
                    var wPlain = new OpcUaBinaryWriter(msPlain);
                    sequenceHeader.Encode(wPlain);
                    wPlain.WriteBytes(bodyBytes);
                    if (paddingSize > 0)
                    {
                        byte paddingByte = (byte)(paddingSize - 1);
                        for (int i = 0; i < paddingSize; i++) wPlain.WriteByte(paddingByte);
                    }
                    // Sig placeholder skip
                }

                var msgHeader = new SecureConversationMessageHeader
                {
                    MessageType = "OPN",
                    ChunkType = 'F',
                    MessageSize = (uint)finalMessageSize,
                    SecureChannelId = SecureChannelId
                };

                byte[] headerBytes;
                using (var msH = new MemoryStream())
                {
                    var wH = new OpcUaBinaryWriter(msH);
                    msgHeader.Encode(wH);
                    headerBytes = msH.ToArray();
                }

                // Sign & Encrypt Logic
                if (signatureSize > 0)
                {
                    int bytesToSignLen = headerBytes.Length + securityHeaderBytes.Length + (totalPlainTextSize - signatureSize);
                    byte[] dataToSign = new byte[bytesToSignLen];
                    int offset = 0;
                    Array.Copy(headerBytes, 0, dataToSign, offset, headerBytes.Length); offset += headerBytes.Length;
                    Array.Copy(securityHeaderBytes, 0, dataToSign, offset, securityHeaderBytes.Length); offset += securityHeaderBytes.Length;
                    Array.Copy(plainTextBlob, 0, dataToSign, offset, totalPlainTextSize - signatureSize);

                    byte[] signature = _securityPolicy.Sign(dataToSign);
                    Array.Copy(signature, 0, plainTextBlob, totalPlainTextSize - signatureSize, signatureSize);
                }

                byte[] encryptedBlob = _securityPolicy.EncryptAsymmetric(plainTextBlob);

                // Senden
                using (var msFinal = new MemoryStream())
                {
                    var wFinal = new OpcUaBinaryWriter(msFinal);
                    wFinal.WriteBytes(headerBytes);
                    wFinal.WriteBytes(securityHeaderBytes);
                    wFinal.WriteBytes(encryptedBlob);
                    byte[] packet = msFinal.ToArray();
                    await _stream.WriteAsync(packet, cancellationToken);
                }

                await ReceiveOpenSecureChannelResponse(cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }

        private void StartRenewalLoop(uint lifetimeMs, CancellationToken cancellationToken)
        {
            // kill old loop
            _renewCts?.Cancel();
            _renewCts = new CancellationTokenSource();

            // if lifetime is 0, set default 1 hour
            if (lifetimeMs == 0) lifetimeMs = 3600000;

            // renew after 75% of lifetime
            int delayMs = (int)(lifetimeMs * 0.75);

            _renewTask = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs, _renewCts.Token);
                    if (!_renewCts.IsCancellationRequested)
                    {
                        await RenewSecureChannelAsync(cancellationToken);
                    }
                }
                catch (TaskCanceledException) { }
                catch (Exception)
                {
                    /// TODO: Disconnect? Handle...
                }
            }, _renewCts.Token);
        }

        private async Task PerformHandshakeAsync(string url, CancellationToken cancellationToken)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected to server.");

            var hello = new TcpHelloMessage(url);

            byte[] sendBuffer;
            using (var ms = new MemoryStream())
            {
                var writer = new OpcUaBinaryWriter(ms);
                hello.Encode(writer);
                sendBuffer = ms.ToArray();
            }

            await _stream.WriteAsync(sendBuffer, cancellationToken);

            byte[] headerBuffer = new byte[8];
            await ReadExactAsync(_stream, headerBuffer, 8, cancellationToken);

            using var msHeader = new MemoryStream(headerBuffer);
            var reader = new OpcUaBinaryReader(msHeader);
            uint type = reader.ReadUInt32(); // HELF / ACKF etc.
            uint length = reader.ReadUInt32();

            if (length < 8) throw new Exception("Invalid Message Size");
            uint bodyLength = length - 8;

            byte[] bodyBuffer = new byte[bodyLength];
            await ReadExactAsync(_stream, bodyBuffer, (int)bodyLength, cancellationToken);

            using var msBody = new MemoryStream(bodyBuffer);
            var bodyReader = new OpcUaBinaryReader(msBody);
            msHeader.Position = 0;
            byte b1 = (byte)msHeader.ReadByte();
            byte b2 = (byte)msHeader.ReadByte();
            byte b3 = (byte)msHeader.ReadByte();

            if (b1 == 'A' && b2 == 'C' && b3 == 'K')
            {
                var ack = new TcpAcknowledgeMessage();
                ack.Decode(bodyReader);

                this.SendBufferSize = ack.SendBufferSize;
                this.ReceiveBufferSize = ack.ReceiveBufferSize;
                this.MaxMessageSize = ack.MaxMessageSize;
                this.MaxChunkCount = ack.MaxChunkCount;
            }
            else if (b1 == 'E' && b2 == 'R' && b3 == 'R')
            {
                var err = new TcpErrorMessage();
                err.Decode(bodyReader);
                throw new Exception($"OPC UA Connection Error: {err.Reason} (0x{err.ErrorCode:X8})");
            }
            else
            {
                throw new Exception("Unknown Message Type during Handshake");
            }
        }

        private async Task OpenSecureChannelAsync(CancellationToken cancellationToken)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected to server.");

            var request = new OpenSecureChannelRequest
            {
                RequestHeader = CreateRequestHeader(),
                ClientProtocolVersion = 0,
                RequestType = SecurityTokenRequestType.Issue,
                SecurityMode = _securityMode,
                ClientNonce = _clientNonce,
                RequestedLifetime = 3600000
            };

            byte[] bodyBytes;
            using (var msBody = new MemoryStream())
            {
                var writerBody = new OpcUaBinaryWriter(msBody);
                request.Encode(writerBody);
                bodyBytes = msBody.ToArray();
            }

            byte[]? senderCertData = null;
            byte[]? receiverThumbprint = null;

            if (_securityMode != MessageSecurityMode.None)
            {
                senderCertData = _clientCertificate?.RawData;
                receiverThumbprint = _serverCertificate?.GetCertHash(); // SHA1 Hash
            }

            var securityHeader = new AsymmetricAlgorithmSecurityHeader
            {
                SecurityPolicyUri = _securityPolicy.SecurityPolicyUri,
                SenderCertificate = senderCertData,
                ReceiverCertificateThumbprint = receiverThumbprint
            };

            byte[] securityHeaderBytes;
            using (var msh = new MemoryStream())
            {
                var wh = new OpcUaBinaryWriter(msh);
                securityHeader.Encode(wh);
                securityHeaderBytes = msh.ToArray();
            }

            var sequenceHeader = new SequenceHeader
            {
                SequenceNumber = _sequenceNumber++,
                RequestId = (uint)_requestId
            };

            // Plaintext to Encrypt = SequenceHeader(8) + Body + Padding + Signature

            int plainContentSize = 8 + bodyBytes.Length; // Sequence + Body
            int signatureSize = _securityPolicy.AsymmetricSignatureSize;
            int inputBlockSize = _securityPolicy.AsymmetricEncryptionBlockSize; // z.B. 214 wtih RSA 2048 Basic256Sha256
            int outputBlockSize = _securityPolicy.AsymmetricCipherTextBlockSize; // z.B. 256 with RSA 2048

            int paddingSize = PaddingCalculator.CalculatePaddingSize(plainContentSize, signatureSize, inputBlockSize);

            int totalPlainTextSize = plainContentSize + paddingSize + signatureSize;

            if (totalPlainTextSize % inputBlockSize != 0)
                throw new Exception("Padding calculation failed. PlainText size is not multiple of BlockSize.");

            int chunks = totalPlainTextSize / inputBlockSize;
            int encryptedDataSize = chunks * outputBlockSize;

            int finalMessageSize = 12 + securityHeaderBytes.Length + encryptedDataSize;

            byte[] plainTextBlob = new byte[totalPlainTextSize];
            using (var msPlain = new MemoryStream(plainTextBlob))
            {
                var wPlain = new OpcUaBinaryWriter(msPlain);
                // Sequence Header
                sequenceHeader.Encode(wPlain);
                // Body
                wPlain.WriteBytes(bodyBytes);
                // Padding
                if (paddingSize > 0)
                {
                    byte paddingByte = (byte)(paddingSize - 1);
                    for (int i = 0; i < paddingSize; i++) wPlain.WriteByte(paddingByte);
                }
                // Signature (placeholder)
                if (signatureSize > 0) wPlain.WriteBytes(new byte[signatureSize]);
            }

            var msgHeader = new SecureConversationMessageHeader
            {
                MessageType = "OPN",
                ChunkType = 'F',
                MessageSize = (uint)finalMessageSize,
                SecureChannelId = SecureChannelId
            };

            byte[] headerBytes;
            using (var msH = new MemoryStream())
            {
                var wH = new OpcUaBinaryWriter(msH);
                msgHeader.Encode(wH);
                headerBytes = msH.ToArray();
            }

            if (signatureSize > 0)
            {
                // [Header] + [SecurityHeader] + [PlainTextBlob (without Sig)]

                int bytesToSignLen = headerBytes.Length + securityHeaderBytes.Length + (totalPlainTextSize - signatureSize);
                byte[] dataToSign = new byte[bytesToSignLen];

                int offset = 0;
                Array.Copy(headerBytes, 0, dataToSign, offset, headerBytes.Length); offset += headerBytes.Length;
                Array.Copy(securityHeaderBytes, 0, dataToSign, offset, securityHeaderBytes.Length); offset += securityHeaderBytes.Length;
                // Plaintext blob minus signature placeholder
                Array.Copy(plainTextBlob, 0, dataToSign, offset, totalPlainTextSize - signatureSize);

                // sign
                byte[] signature = _securityPolicy.Sign(dataToSign);

                // copy signature into plainTextBlob
                Array.Copy(signature, 0, plainTextBlob, totalPlainTextSize - signatureSize, signatureSize);
            }

            byte[] encryptedBlob = _securityPolicy.EncryptAsymmetric(plainTextBlob);

            if (encryptedBlob.Length != encryptedDataSize)
                throw new Exception($"Encryption size mismatch. Expected {encryptedDataSize}, got {encryptedBlob.Length}");

            using (var msFinal = new MemoryStream())
            {
                var wFinal = new OpcUaBinaryWriter(msFinal);
                wFinal.WriteBytes(headerBytes);
                wFinal.WriteBytes(securityHeaderBytes);
                wFinal.WriteBytes(encryptedBlob);

                byte[] packet = msFinal.ToArray();
                await _stream.WriteAsync(packet, cancellationToken);
            }

            await ReceiveOpenSecureChannelResponse(cancellationToken);
        }

        private async Task ReceiveOpenSecureChannelResponse(CancellationToken cancellationToken)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected to server.");

            byte[] headerBase = new byte[8];
            await ReadExactAsync(_stream, headerBase, 8, cancellationToken);

            string msgType;
            uint msgSize;

            using (var msH = new MemoryStream(headerBase))
            {
                var rH = new OpcUaBinaryReader(msH);
                msgType = new string([(char)rH.ReadByte(), (char)rH.ReadByte(), (char)rH.ReadByte()]);
                char chunkType = (char)rH.ReadByte();
                msgSize = rH.ReadUInt32();
            }

            if (msgType == "ERR")
            {
                int errLen = (int)(msgSize - 8);
                byte[] errBody = new byte[errLen];
                await ReadExactAsync(_stream, errBody, errLen, cancellationToken);

                using var msErr = new MemoryStream(errBody);
                var rErr = new OpcUaBinaryReader(msErr);
                var err = new TcpErrorMessage();
                err.Decode(rErr);
                throw new Exception($"OPC UA Error: {err.Reason} (0x{err.ErrorCode:X8})");
            }

            if (msgType != "OPN") throw new Exception($"Expected OPN, got {msgType}");

            byte[] channelIdBuf = new byte[4];
            await ReadExactAsync(_stream, channelIdBuf, 4, cancellationToken);

            SecureChannelId = BinaryPrimitives.ReadUInt32LittleEndian(channelIdBuf);

            // Body Size = Total Size - 12 Bytes Header (8 Base + 4 ChannelId)
            int bodyLen = (int)(msgSize - 12);
            byte[] rawBody = new byte[bodyLen];
            await ReadExactAsync(_stream, rawBody, bodyLen, cancellationToken);

            using var msRaw = new MemoryStream(rawBody);
            var rRaw = new OpcUaBinaryReader(msRaw);
            long startPos = msRaw.Position;
            var secHeader = new AsymmetricAlgorithmSecurityHeader
            {
                SecurityPolicyUri = rRaw.ReadString(),
                SenderCertificate = rRaw.ReadByteString(),
                ReceiverCertificateThumbprint = rRaw.ReadByteString()
            };
            long endHeaderPos = msRaw.Position;

            int secHeaderLen = (int)(endHeaderPos - startPos);
            byte[] secHeaderBytes = new byte[secHeaderLen];
            Array.Copy(rawBody, startPos, secHeaderBytes, 0, secHeaderLen);

            if (_securityMode != MessageSecurityMode.None)
            {
                if (secHeader.SenderCertificate == null)
                {
                    throw new InvalidDataException("Sender certificate must not be null when MessageSecurityMode is different than None.");
                }
                /// TODO: allow configuration regarding untrusted authority
                ValidateServerCertificate(secHeader.SenderCertificate, true);
            }

            byte[] decryptedPayload;

            if (_securityMode != MessageSecurityMode.None)
            {
                int encryptedLen = (int)(msRaw.Length - endHeaderPos);
                byte[] encryptedData = new byte[encryptedLen];
                Array.Copy(rawBody, endHeaderPos, encryptedData, 0, encryptedLen);

                byte[] plainBlock = _securityPolicy.DecryptAsymmetric(encryptedData);

                int sigSize = _securityPolicy.AsymmetricSignatureSize;
                int dataLen = plainBlock.Length - sigSize;
                byte[] signature = new byte[sigSize];
                Array.Copy(plainBlock, dataLen, signature, 0, sigSize);

                // [BaseHeader(8)] + [ChannelId(4)] + [SecHeader] + [PlainBlockWithoutSig]
                int signedDataLen = 8 + 4 + secHeaderBytes.Length + dataLen;
                byte[] signedData = new byte[signedDataLen];
                int off = 0;
                Array.Copy(headerBase, 0, signedData, off, 8); off += 8;
                Array.Copy(channelIdBuf, 0, signedData, off, 4); off += 4;
                Array.Copy(secHeaderBytes, 0, signedData, off, secHeaderBytes.Length); off += secHeaderBytes.Length;
                Array.Copy(plainBlock, 0, signedData, off, dataLen);

                if (!_securityPolicy.Verify(signedData, signature))
                    throw new Exception("Signature Verification Failed");

                byte paddingByte = plainBlock[dataLen - 1];
                int paddingSize = paddingByte + 1;
                int cleanPayloadLen = dataLen - paddingSize;
                decryptedPayload = new byte[cleanPayloadLen];
                Array.Copy(plainBlock, 0, decryptedPayload, 0, cleanPayloadLen);
            }
            else
            {
                int restLen = (int)(msRaw.Length - endHeaderPos);
                decryptedPayload = new byte[restLen];
                Array.Copy(rawBody, endHeaderPos, decryptedPayload, 0, restLen);
            }

            // 6. Body
            using var msDec = new MemoryStream(decryptedPayload);
            var rDec = new OpcUaBinaryReader(msDec);
            var seqHeader = new SequenceHeader
            {
                SequenceNumber = rDec.ReadUInt32(),
                RequestId = rDec.ReadUInt32()
            };

            NodeId typeId = NodeId.Decode(rDec);
            if (typeId.NumericIdentifier != 449) throw new Exception($"Expected OpenSecureChannelResponse (449), got {typeId.NumericIdentifier}");

            var response = new OpenSecureChannelResponse();
            response.Decode(rDec);

            if (response.ResponseHeader?.ServiceResult != 0)
                throw new Exception($"OpenSecureChannel ServiceResult: 0x{response.ResponseHeader?.ServiceResult:X8}");

            if (response.SecurityToken == null)
                throw new InvalidDataException("OpenSecureChannelResponse has no SecurityToken.");

            _tokenId = response.SecurityToken.TokenId;

            if (response.ServerNonce != null && _clientNonce != null)
            {
                _securityPolicy.DeriveKeys(_clientNonce, response.ServerNonce);
            }

            StartRenewalLoop(response.SecurityToken.RevisedLifetime, cancellationToken);
        }

        public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TResponse : new()
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected to server.");

            await _lock.WaitAsync(cancellationToken);
            try
            {
                byte[] bodyBytes;
                using (var ms = new MemoryStream())
                {
                    var w = new OpcUaBinaryWriter(ms);
                    if (request is GetEndpointsRequest ge) ge.Encode(w);
                    else if (request is CreateSessionRequest cs) cs.Encode(w);
                    else if (request is ActivateSessionRequest asr) asr.Encode(w);
                    else if (request is ReadRequest rr) rr.Encode(w);
                    else if (request is WriteRequest wr) wr.Encode(w);
                    else if (request is BrowseRequest br) br.Encode(w);
                    else if (request is CreateSubscriptionRequest csr) csr.Encode(w);
                    else if (request is CreateMonitoredItemsRequest cmir) cmir.Encode(w);
                    else if (request is PublishRequest pr) pr.Encode(w);
                    else if (request is CloseSessionRequest csr2) csr2.Encode(w);
                    else if (request is CloseSecureChannelRequest cscr) cscr.Encode(w);
                    else if (request is CallRequest cr) cr.Encode(w);
                    else if (request is BrowseNextRequest bcr) bcr.Encode(w);
                    else if (request is DeleteMonitoredItemsRequest dmir) dmir.Encode(w);
                    else if (request is DeleteSubscriptionsRequest dsr) dsr.Encode(w);
                    else if (request is ModifyMonitoredItemsRequest mmir) mmir.Encode(w);
                    else if (request is SetMonitoringModeRequest smmr) smmr.Encode(w);
                    else if (request is SetPublishingModeRequest spmr) spmr.Encode(w);
                    else if (request is TranslateBrowsePathsToNodeIdsRequest tbptnidr) tbptnidr.Encode(w);
                    else throw new NotImplementedException($"Request Type {typeof(TRequest).Name} not supported.");

                    bodyBytes = ms.ToArray();
                }

                // Header from MSG:
                // [SecureConversationMessageHeader (12)]
                // [SymmetricSecurityHeader (4)] (TokenId)
                // [SequenceHeader (8)]
                // [Body]
                // [Padding]
                // [Signature]

                int plainTextContentSize = 8 + bodyBytes.Length; // SequenceHeader + Body
                int signatureSize = _securityPolicy.SymmetricSignatureSize; // HMAC size
                int blockSize = _securityPolicy.SymmetricBlockSize; // CBC Block size

                int paddingSize = PaddingCalculator.CalculatePaddingSize(plainTextContentSize, signatureSize, blockSize);

                int totalSize = 12 + 4 + plainTextContentSize + paddingSize + signatureSize;

                using (var msFinal = new MemoryStream(totalSize))
                {
                    var w = new OpcUaBinaryWriter(msFinal);
                    // Header MSG
                    var msgHeader = new SecureConversationMessageHeader
                    {
                        MessageType = request is CloseSecureChannelRequest ? "CLO" : "MSG",
                        ChunkType = 'F',
                        MessageSize = (uint)totalSize,
                        SecureChannelId = SecureChannelId
                    };

                    msgHeader.Encode(w);

                    // Symmetric Security Header (TokenId only)
                    w.WriteUInt32(_tokenId);

                    // Sequence Header (Start Encryption)
                    long cryptoStart = msFinal.Position;
                    var nextHandle = (uint)Interlocked.Increment(ref _requestId);
                    var seqHeader = new SequenceHeader { SequenceNumber = _sequenceNumber++, RequestId = nextHandle };
                    seqHeader.Encode(w);

                    // Body
                    w.WriteBytes(bodyBytes);

                    // Padding
                    if (paddingSize > 0)
                    {
                        byte paddingByte = (byte)(paddingSize - 1);
                        for (int i = 0; i < paddingSize; i++) w.WriteByte(paddingByte);
                    }

                    // Signature place holder
                    long sigPos = msFinal.Position;
                    if (signatureSize > 0) w.WriteBytes(new byte[signatureSize]);

                    // --- Crypto ---
                    byte[] raw = msFinal.ToArray();

                    // F. Sign (Header + Body + Padding)
                    if (signatureSize > 0)
                    {
                        byte[] dataToSign = new byte[sigPos];
                        Array.Copy(raw, 0, dataToSign, 0, sigPos);

                        byte[] signature = _securityPolicy.SignSymmetric(dataToSign);

                        if (signature.Length != signatureSize)
                            throw new Exception("Symmetric Signature size mismatch");

                        Array.Copy(signature, 0, raw, sigPos, signatureSize);
                    }

                    // G. Encryption
                    // MsgHeader(12) + TokenId(4) = 16.
                    if (_securityMode == MessageSecurityMode.SignAndEncrypt)
                    {
                        // MsgHeader(12) + TokenId(4) = 16.
                        int cryptoStartOffset = 16;
                        int encryptLen = raw.Length - cryptoStartOffset;

                        byte[] dataToEncrypt = new byte[encryptLen];
                        Array.Copy(raw, cryptoStartOffset, dataToEncrypt, 0, encryptLen);

                        byte[] encryptedData = _securityPolicy.EncryptSymmetric(dataToEncrypt);
                        Array.Copy(encryptedData, 0, raw, cryptoStartOffset, encryptedData.Length);
                    }

                    await _stream.WriteAsync(raw, cancellationToken);
                }

                // 4. Response
                return await ReceiveResponseAsync<TResponse>(cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<TResponse> ReceiveResponseAsync<TResponse>(CancellationToken cancellationToken) where TResponse : new()
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected to server.");

            byte[] headerBase = new byte[8];
            await ReadExactAsync(_stream, headerBase, 8, cancellationToken);

            string msgType;
            uint msgSize;

            using (var msH = new MemoryStream(headerBase))
            {
                var rH = new OpcUaBinaryReader(msH);
                msgType = new string([(char)rH.ReadByte(), (char)rH.ReadByte(), (char)rH.ReadByte()]);
                char chunkType = (char)rH.ReadByte();
                msgSize = rH.ReadUInt32();
            }

            if (msgType == "ERR")
            {
                int errLen = (int)(msgSize - 8);
                byte[] errBody = new byte[errLen];
                await ReadExactAsync(_stream, errBody, errLen, cancellationToken);

                using var msErr = new MemoryStream(errBody);
                var rErr = new OpcUaBinaryReader(msErr);
                var err = new TcpErrorMessage();
                err.Decode(rErr);
                throw new Exception($"OPC UA Error: {err.Reason} (0x{err.ErrorCode:X8})");
            }

            if (msgType != "MSG") throw new Exception($"Expected MSG, got {msgType}");

            byte[] channelIdBuf = new byte[4];
            await ReadExactAsync(_stream, channelIdBuf, 4, cancellationToken);
            ///TODO: Check channel id

            // Body Size = Total Size - 12 Bytes Header
            int totalBodyLen = (int)(msgSize - 12);
            byte[] fullBody = new byte[totalBodyLen];
            await ReadExactAsync(_stream, fullBody, totalBodyLen, cancellationToken);

            byte[] decryptedBytes;
            int cryptoStartOffset = 4; // TokenId Size

            if (_securityMode == MessageSecurityMode.SignAndEncrypt)
            {
                int encryptedLen = totalBodyLen - cryptoStartOffset;
                byte[] cipherText = new byte[encryptedLen];
                Array.Copy(fullBody, cryptoStartOffset, cipherText, 0, encryptedLen);

                byte[] plainTextPart = _securityPolicy.DecryptSymmetric(cipherText);

                int sigSize = _securityPolicy.SymmetricSignatureSize;
                int dataLen = plainTextPart.Length - sigSize;
                byte[] signature = new byte[sigSize];
                Array.Copy(plainTextPart, dataLen, signature, 0, sigSize);

                // [BaseHeader(8)] + [ChannelId(4)] + [TokenId(4)] + [PlainPartWithoutSig]
                int signedDataLen = 8 + 4 + 4 + dataLen;
                byte[] dataToVerify = new byte[signedDataLen];
                int off = 0;
                Array.Copy(headerBase, 0, dataToVerify, off, 8); off += 8;
                Array.Copy(channelIdBuf, 0, dataToVerify, off, 4); off += 4;
                Array.Copy(fullBody, 0, dataToVerify, off, 4); off += 4; // TokenId
                Array.Copy(plainTextPart, 0, dataToVerify, off, dataLen);

                if (!_securityPolicy.VerifySymmetric(dataToVerify, signature))
                {
                    throw new Exception("Symmetric Signature Verification Failed!");
                }

                byte paddingByte = plainTextPart[dataLen - 1];
                int paddingSize = paddingByte + 1;
                int cleanPayloadLen = dataLen - paddingSize;
                decryptedBytes = new byte[cleanPayloadLen];
                Array.Copy(plainTextPart, 0, decryptedBytes, 0, cleanPayloadLen);
            }
            else if (_securityMode == MessageSecurityMode.Sign)
            {
                int sigSize = _securityPolicy.SymmetricSignatureSize;

                // Structure: [TokenId(4)] [SequenceHeader] [Body] [Signature]
                // fullBody: [TokenId] + [Rest]

                int dataLen = totalBodyLen - 4 - sigSize; // Length of Sequence + Body
                if (dataLen < 0) throw new Exception("Message too short for signature.");

                byte[] signature = new byte[sigSize];
                Array.Copy(fullBody, 4 + dataLen, signature, 0, sigSize);

                // Verify: [BaseHeader(8)] + [ChannelId(4)] + [TokenId(4)] + [Sequence+Body]
                int signedDataLen = 8 + 4 + 4 + dataLen;
                byte[] dataToVerify = new byte[signedDataLen];
                int off = 0;
                Array.Copy(headerBase, 0, dataToVerify, off, 8); off += 8;
                Array.Copy(channelIdBuf, 0, dataToVerify, off, 4); off += 4;
                // TokenId + Data
                Array.Copy(fullBody, 0, dataToVerify, off, 4 + dataLen);

                if (!_securityPolicy.VerifySymmetric(dataToVerify, signature))
                {
                    throw new Exception("Symmetric Signature Verification Failed!");
                }

                // Payload (Sequence + Body)
                decryptedBytes = new byte[dataLen];
                Array.Copy(fullBody, 4, decryptedBytes, 0, dataLen);
            }
            else
            {
                // None: [TokenId] [Sequence] [Body]
                int restLen = totalBodyLen - 4;
                decryptedBytes = new byte[restLen];
                Array.Copy(fullBody, 4, decryptedBytes, 0, restLen);
            }

            // 5. Deserialisieren
            using var ms = new MemoryStream(decryptedBytes);
            var r = new OpcUaBinaryReader(ms);
            // Sequence Header
            uint seqNum = r.ReadUInt32();
            uint reqId = r.ReadUInt32();

            // Body TypeId (NodeId)
            NodeId typeId = NodeId.Decode(r);

            // --- GLOBAL SERVICE FAULT CHECK ---
            // ID 397 = ServiceFault
            if (typeId.NumericIdentifier == 397)
            {
                var faultHeader = ResponseHeader.Decode(r);

                throw new Exception($"Server returned ServiceFault: 0x{faultHeader.ServiceResult:X8}");
            }
            // ------------------------------------

            var response = new TResponse();

            if (response is GetEndpointsResponse ger)
            {
                if (typeId.NumericIdentifier != GetEndpointsResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for GetEndpoints: {typeId.NumericIdentifier}");
                ger.Decode(r);
            }
            else if (response is CreateSessionResponse csr)
            {
                if (typeId.NumericIdentifier != CreateSessionResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for CreateSession: {typeId.NumericIdentifier}");
                csr.Decode(r);
            }
            else if (response is ActivateSessionResponse asr)
            {
                if (typeId.NumericIdentifier != ActivateSessionResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for ActivateSession: {typeId.NumericIdentifier}");
                asr.Decode(r);
            }
            else if (response is ReadResponse rr)
            {
                if (typeId.NumericIdentifier != ReadResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for ReadResponse: {typeId.NumericIdentifier}");
                rr.Decode(r);
            }
            else if (response is WriteResponse wr)
            {
                if (typeId.NumericIdentifier != WriteResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for WriteResponse: {typeId.NumericIdentifier}");
                wr.Decode(r);
            }
            else if (response is BrowseResponse br)
            {
                if (typeId.NumericIdentifier != BrowseResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for BrowseResponse: {typeId.NumericIdentifier}");
                br.Decode(r);
            }
            else if (response is CreateSubscriptionResponse csr2)
            {
                if (typeId.NumericIdentifier != CreateSubscriptionResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for CreateSubscriptionResponse: {typeId.NumericIdentifier}");
                csr2.Decode(r);
            }
            else if (response is CreateMonitoredItemsResponse cmir)
            {
                if (typeId.NumericIdentifier != CreateMonitoredItemsResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for CreateMonitoredItemResponse: {typeId.NumericIdentifier}");
                cmir.Decode(r);
            }
            else if (response is PublishResponse pr)
            {
                if (typeId.NumericIdentifier != PublishResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for PublishResponse: {typeId.NumericIdentifier}");
                pr.Decode(r);
            }
            else if (response is CloseSessionResponse csr3)
            {
                if (typeId.NumericIdentifier != CloseSessionResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for CloseSessionResponse: {typeId.NumericIdentifier}");
                csr3.Decode(r);
            }
            else if (response is CloseSecureChannelResponse cscr)
            {
                if (typeId.NumericIdentifier != CloseSecureChannelResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for CloseSecureChannelResponse: {typeId.NumericIdentifier}");
                cscr.Decode(r);
            }
            else if (response is CallResponse cr)
            {
                if (typeId.NumericIdentifier != CallResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for CallResponse: {typeId.NumericIdentifier}");
                cr.Decode(r);
            }
            else if (response is BrowseNextResponse bnr)
            {
                if (typeId.NumericIdentifier != BrowseNextResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for BrowseNextResponse: {typeId.NumericIdentifier}");
                bnr.Decode(r);
            }
            else if (response is DeleteMonitoredItemsResponse dmir)
            {
                if (typeId.NumericIdentifier != DeleteMonitoredItemsResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for DeleteMonitoredItemsResponse: {typeId.NumericIdentifier}");
                dmir.Decode(r);
            }
            else if (response is DeleteSubscriptionsResponse dsr)
            {
                if (typeId.NumericIdentifier != DeleteSubscriptionsResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for DeleteSubscriptionsResponse: {typeId.NumericIdentifier}");
                dsr.Decode(r);
            }
            else if (response is SetMonitoringModeResponse smmr)
            {
                if (typeId.NumericIdentifier != SetMonitoringModeResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for SetMonitoringModeResponse: {typeId.NumericIdentifier}");
                smmr.Decode(r);
            }
            else if (response is SetPublishingModeResponse spmr)
            {
                if (typeId.NumericIdentifier != SetPublishingModeResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for SetPublishingModeResponse: {typeId.NumericIdentifier}");
                spmr.Decode(r);
            }
            else if (response is ModifyMonitoredItemsResponse mmir)
            {
                if (typeId.NumericIdentifier != ModifyMonitoredItemsResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for ModifyMonitoredItemsResponse: {typeId.NumericIdentifier}");
                mmir.Decode(r);
            }
            else if (response is TranslateBrowsePathsToNodeIdsResponse tbptnir)
            {
                if (typeId.NumericIdentifier != TranslateBrowsePathsToNodeIdsResponse.NodeId.NumericIdentifier)
                    throw new Exception($"Unexpected Response Type for TranslateBrowsePathsToNodeIdsResponse: {typeId.NumericIdentifier}");
                tbptnir.Decode(r);
            }
            else
            {
                throw new NotImplementedException($"Response decoding for {typeof(TResponse).Name} not implemented.");
            }

            return response;
        }

        public async Task<StatusCode[]?> DeleteMonitoredItemsAsync(uint subscriptionId, uint[] monitoredItemIds, CancellationToken token = default)
        {
            var req = new DeleteMonitoredItemsRequest
            {
                RequestHeader = CreateRequestHeader(),
                SubscriptionId = subscriptionId,
                MonitoredItemIds = monitoredItemIds
            };
            var resp = await SendRequestAsync<DeleteMonitoredItemsRequest, DeleteMonitoredItemsResponse>(req, token);
            return resp.Results;
        }

        public async Task<StatusCode[]?> DeleteSubscriptionsAsync(uint[] subscriptionIds, CancellationToken token = default)
        {
            var req = new DeleteSubscriptionsRequest
            {
                RequestHeader = CreateRequestHeader(),
                SubscriptionIds = subscriptionIds
            };
            var resp = await SendRequestAsync<DeleteSubscriptionsRequest, DeleteSubscriptionsResponse>(req, token);
            return resp.Results;
        }

        public async Task<GetEndpointsResponse> GetEndpointsAsync(CancellationToken cancellationToken = default)
        {
            var req = new GetEndpointsRequest
            {
                EndpointUrl = _endpointUrl,
                RequestHeader = CreateRequestHeader(),
            };

            return await SendRequestAsync<GetEndpointsRequest, GetEndpointsResponse>(req, cancellationToken);
        }

        private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken cancellationToken)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead), cancellationToken);
                if (read == 0) throw new EndOfStreamException("Connection closed by remote host.");
                totalRead += read;
            }
            return totalRead;
        }

        private void ValidateServerSessionSignature(CreateSessionResponse response, byte[] clientNonce)
        {
            var sigData = response.ServerSignature;

            // 1. No signature -> skip check (e.g. SecurityMode None)
            if (sigData == null || sigData.Signature == null || sigData.Signature.Length == 0 || sigData.Algorithm == null)
            {
                // if SecurityMode requires signature, throw
                if (_securityMode != MessageSecurityMode.None)
                {
                    throw new Exception("Server did not send a signature, but SecurityMode requires it.");
                }
                return;
            }

            // 2. Recreate signed data
            // Spec Part 4: ClientCertificate + ClientNonce
            byte[] clientCertBytes = _clientCertificate?.RawData ?? [];

            byte[] dataToVerify = new byte[clientCertBytes.Length + clientNonce.Length];
            Array.Copy(clientCertBytes, 0, dataToVerify, 0, clientCertBytes.Length);
            Array.Copy(clientNonce, 0, dataToVerify, clientCertBytes.Length, clientNonce.Length);

            // 3. Retrieve Server Public Key
            X509Certificate2? serverCertToVerify = _serverCertificate ?? throw new Exception("Cannot verify Server Signature: No Server Certificate available.");

            // 4. Verify Signature
            using var rsa = serverCertToVerify.GetRSAPublicKey() ?? throw new Exception("Server Certificate has no Public Key.");
            var (hashAlg, padding) = CryptoUtils.ParseAlgorithm(sigData.Algorithm);

            if (!rsa.VerifyData(dataToVerify, sigData.Signature, hashAlg, padding))
            {
                throw new Exception("CRITICAL: Server Session Signature Invalid!");
            }
        }

        private void ValidateServerCertificate(byte[] certBytes, bool allowUnknownAuthority = false)
        {
            /// TODO: add general "allow untrusted"

            if (certBytes == null || certBytes.Length == 0) return;

            X509Certificate2? cert = null;

            // Standard X.509 DER (Raw Certificate)
            try
            {
                cert = X509CertificateLoader.LoadCertificate(certBytes);
            }
            catch (CryptographicException)
            {
                throw new Exception($"Could not decode Server Certificate. Raw Length: {certBytes.Length} bytes.");
            }

            if (cert == null)
            {
                throw new Exception($"Could not decode Server Certificate. Raw Length: {certBytes.Length} bytes.");
            }

            var host = new Uri(_endpointUrl).Host;

            // 1. OS validation (Chain Build)
            using var chain = new X509Chain();
            // default: no online check
            /// TODO: allow configuration: allow check for online revocation
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

            if (allowUnknownAuthority)
                chain.ChainPolicy.VerificationFlags |= X509VerificationFlags.AllowUnknownCertificateAuthority;

            bool osValid = chain.Build(cert);

            // 2. Fire event
            var args = new CertificateValidationEventArgs(cert, host, chain.ChainStatus, osValid);

            // If no listeners, the OS decides
            if (CertificateValidation == null)
            {
                if (!osValid)
                {
                    string errors = string.Join(", ", Array.ConvertAll(chain.ChainStatus, s => s.StatusInformation));
                    throw new Exception($"Server Certificate rejected by OS: {errors}");
                }
            }
            else
            {
                // Invoke
                CertificateValidation.Invoke(this, args);

                if (!args.Accept)
                {
                    throw new Exception("Server Certificate rejected by user application.");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            try
            {
                DisconnectAsync().Wait(2000);
            }
            catch { }

            _stream?.Dispose();
            _client?.Dispose();
            _renewCts?.Cancel();
            GC.SuppressFinalize(this);
        }
    }
}