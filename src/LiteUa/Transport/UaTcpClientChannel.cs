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
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LiteUa.Transport
{
    /// <summary>
    /// Represents a client channel for OPC UA communication over TCP.
    /// </summary>
    public class UaTcpClientChannel : IUaTcpClientChannel, IDisposable, IAsyncDisposable
    {
        private TcpClient? _client;
        private Stream? _stream;
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

        private readonly ConcurrentDictionary<uint, PendingRequest> _pendingRequests = new();
        private Task? _responseProcessorTask;
        private CancellationTokenSource? _channelCts;
        private bool _isClosing = false;

        // --- Renewal Management ---
        private CancellationTokenSource? _renewCts;

        private uint _heartbeatIntervalMs;
        private readonly NodeId _heartbeatNodeId = new(2258);
        private readonly uint _heartbeatTimeoutHintMs;
        private CancellationTokenSource? _heartbeatCts;
        private Task? _heartbeatTask;

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

        /// <summary>
        /// Creates a new instance of the <see cref="UaTcpClientChannel"/> class.
        /// </summary>
        /// <param name="endpointUrl">The network address of the OPC UA server endpoint (e.g., "opc.tcp://localhost:4840").</param>
        /// <param name="applicationUri">The globally unique identifier (URI) for the client application instance.</param>
        /// <param name="productUri">The globally unique identifier (URI) for the client product.</param>
        /// <param name="applicationName">A human-readable name for the client application.</param>
        /// <param name="policyFactory">The factory responsible for creating the security policy and cryptographic providers.</param>
        /// <param name="securityMode">The message security mode (None, Sign, or SignAndEncrypt) to apply to the channel.</param>
        /// <param name="clientCertificate">The X.509 certificate of the client, used for signing and decryption.</param>
        /// <param name="serverCertificate">The X.509 certificate of the server, used for encryption and signature verification.</param>
        /// <param name="heartbeatIntervalMs"">The interval in milliseconds for sending heartbeat messages to keep the connection alive.</param>
        /// <param name="heartbeatTimeoutHintMs">The timeout hint in milliseconds for heartbeat responses.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="endpointUrl"/>, <paramref name="applicationUri"/>, <paramref name="productUri"/>, <paramref name="applicationName"/>, or <paramref name="policyFactory"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the string parameters are empty or consist only of white space.</exception>
        public UaTcpClientChannel(
            string endpointUrl,
            string applicationUri,
            string productUri,
            string applicationName,
            ISecurityPolicyFactory policyFactory,
            MessageSecurityMode securityMode,
            X509Certificate2? clientCertificate,
            X509Certificate2? serverCertificate,
            uint heartbeatIntervalMs,
            uint heartbeatTimeoutHintMs)
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
            _heartbeatIntervalMs = heartbeatIntervalMs;
            _heartbeatTimeoutHintMs = heartbeatTimeoutHintMs;
        }

        private async Task ProcessResponsesAsync()
        {
            var token = _channelCts!.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_isClosing && _pendingRequests.IsEmpty) break;

                    byte[] headerBase = new byte[8];
                    await ReadExactAsync(_stream!, headerBase, 8, token);

                    string msgType = System.Text.Encoding.ASCII.GetString(headerBase, 0, 3);
                    uint msgSize = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(headerBase.AsSpan(4));

                    if (msgSize < 12) throw new Exception("Message size too small.");

                    int remainingSize = (int)msgSize - 8;
                    byte[] payload = new byte[remainingSize];
                    await ReadExactAsync(_stream!, payload, remainingSize, token);

                    if (msgType == "ERR")
                    {
                        using var msErr = new MemoryStream(payload);
                        var rErr = new OpcUaBinaryReader(msErr);
                        var err = new TcpErrorMessage();
                        err.Decode(rErr);
                        throw new Exception($"Protocol Error: {err.Reason} (0x{err.ErrorCode:X8})");
                    }

                    byte[] decryptedBytes;
                    uint requestId;

                    if (msgType == "OPN")
                    {
                        using var msOpn = new MemoryStream(payload);
                        var rOpn = new OpcUaBinaryReader(msOpn);
                        msOpn.Seek(4, SeekOrigin.Begin);
                        _ = rOpn.ReadString(); _ = rOpn.ReadByteString(); _ = rOpn.ReadByteString();
                        int headerLen = (int)msOpn.Position;

                        byte[] cipherText = new byte[payload.Length - headerLen];
                        Array.Copy(payload, headerLen, cipherText, 0, cipherText.Length);

                        if (_securityMode != MessageSecurityMode.None)
                        {
                            byte[] plainBlock = _securityPolicy.DecryptAsymmetric(cipherText);
                            requestId = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(plainBlock.AsSpan(4, 4));
                            decryptedBytes = plainBlock;
                        }
                        else
                        {
                            requestId = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(cipherText.AsSpan(4, 4));
                            decryptedBytes = cipherText;
                        }
                    }
                    else
                    {
                        int cryptoOffset = 8; // ChannelId + TokenId
                        int cipherTextLen = payload.Length - cryptoOffset;
                        byte[] cipherText = new byte[cipherTextLen];
                        Array.Copy(payload, cryptoOffset, cipherText, 0, cipherTextLen);

                        if (_securityMode == MessageSecurityMode.SignAndEncrypt)
                        {
                            byte[] plainBlock = _securityPolicy.DecryptSymmetric(cipherText);
                            requestId = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(plainBlock.AsSpan(4, 4));

                            int sigSize = _securityPolicy.SymmetricSignatureSize;
                            int dataLen = plainBlock.Length - sigSize;
                            byte[] signature = new byte[sigSize];
                            Array.Copy(plainBlock, dataLen, signature, 0, sigSize);

                            byte[] verifyBuf = new byte[8 + 8 + dataLen];
                            Array.Copy(headerBase, 0, verifyBuf, 0, 8);
                            Array.Copy(payload, 0, verifyBuf, 8, 8);
                            Array.Copy(plainBlock, 0, verifyBuf, 16, dataLen);

                            if (!_securityPolicy.VerifySymmetric(verifyBuf, signature))
                                throw new Exception("Symmetric Signature Verification Failed.");

                            byte paddingByte = plainBlock[dataLen - 1];
                            decryptedBytes = new byte[dataLen - (paddingByte + 1)];
                            Array.Copy(plainBlock, 0, decryptedBytes, 0, decryptedBytes.Length);
                        }
                        else if (_securityMode == MessageSecurityMode.Sign)
                        {
                            int sigSize = _securityPolicy.SymmetricSignatureSize;
                            int dataLen = cipherText.Length - sigSize;
                            requestId = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(cipherText.AsSpan(4, 4));

                            byte[] signature = new byte[sigSize];
                            Array.Copy(cipherText, dataLen, signature, 0, sigSize);

                            byte[] verifyBuf = new byte[8 + 8 + dataLen];
                            Array.Copy(headerBase, 0, verifyBuf, 0, 8);
                            Array.Copy(payload, 0, verifyBuf, 8, 8);
                            Array.Copy(cipherText, 0, verifyBuf, 16, dataLen);

                            if (!_securityPolicy.VerifySymmetric(verifyBuf, signature))
                                throw new Exception("Symmetric Signature Verification Failed.");

                            decryptedBytes = new byte[dataLen];
                            Array.Copy(cipherText, 0, decryptedBytes, 0, dataLen);
                        }
                        else
                        {
                            requestId = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(cipherText.AsSpan(4, 4));
                            decryptedBytes = cipherText;
                        }
                    }

                    if (_pendingRequests.TryRemove(requestId, out var pending))
                    {
                        pending.Tcs.TrySetResult(decryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_isClosing)
                {
                    foreach (var req in _pendingRequests.Values)
                        req.Tcs.TrySetException(ex);
                    _pendingRequests.Clear();
                    throw;
                }
            }
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
                TimestampsToReturn = TimestampsToReturn.Both,
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
            if (_stream == null) return;

            _isClosing = true;

            _heartbeatCts?.Cancel();
            _renewCts?.Cancel();
            _channelCts?.Cancel();

            try
            {
                if (_heartbeatTask != null) await _heartbeatTask;
                if (_renewTask != null) await _renewTask;
                if (_responseProcessorTask != null) await _responseProcessorTask;
            }
            catch (Exception) { /* Ignore cancellations/shutdown errors */ }

            try
            {
                if (_authenticationToken != null && _authenticationToken.NumericIdentifier != 0)
                {
                    var req = new CloseSessionRequest { RequestHeader = CreateRequestHeader(), DeleteSubscriptions = true };
                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2000));
                    try
                    {
                        await SendRequestAsync<CloseSessionRequest, CloseSessionResponse>(req, cts.Token);
                    }
                    catch { /* Session might already be closed or timed out */ }
                    _authenticationToken = new NodeId(0);
                }

                if (SecureChannelId != 0)
                {
                    var req = new CloseSecureChannelRequest { RequestHeader = CreateRequestHeader() };
                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
                    try
                    {
                        await SendRequestAsync<CloseSecureChannelRequest, CloseSecureChannelResponse>(req, cts.Token);
                    }
                    catch { /* Channel might already be closed by server */ }
                    SecureChannelId = 0;
                }
            }
            catch (Exception)
            {
                // Best effort shutdown
            }

            if (_responseProcessorTask != null)
            {
                try { await _responseProcessorTask; } catch { }
                _responseProcessorTask = null;
            }

            _stream?.Dispose();
            _client?.Dispose();
            _stream = null;
            _client = null;
            _channelCts?.Dispose();
            _heartbeatCts?.Dispose();
            _renewCts?.Dispose();
            _channelCts = null;
            _heartbeatCts = null;
            _renewCts = null;
        }

        private byte[] PrepareOpenSecureChannelPacket(byte[] bodyBytes, uint requestId, uint sequenceNumber)
        {
            byte[]? senderCertData = _securityMode != MessageSecurityMode.None ? _clientCertificate?.RawData : null;
            byte[]? receiverThumbprint = _securityMode != MessageSecurityMode.None ? _serverCertificate?.GetCertHash() : null;

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

            int plainContentSize = 8 + bodyBytes.Length;
            int signatureSize = _securityPolicy.AsymmetricSignatureSize;
            int inputBlockSize = _securityPolicy.AsymmetricEncryptionBlockSize;
            int outputBlockSize = _securityPolicy.AsymmetricCipherTextBlockSize;
            int paddingSize = PaddingCalculator.CalculatePaddingSize(plainContentSize, signatureSize, inputBlockSize);
            int totalPlainTextSize = plainContentSize + paddingSize + signatureSize;

            byte[] plainTextBlob = new byte[totalPlainTextSize];
            using (var msPlain = new MemoryStream(plainTextBlob))
            {
                var wPlain = new OpcUaBinaryWriter(msPlain);
                new SequenceHeader { SequenceNumber = sequenceNumber, RequestId = requestId }.Encode(wPlain);
                wPlain.WriteBytes(bodyBytes);
                if (paddingSize > 0)
                {
                    byte p = (byte)(paddingSize - 1);
                    for (int i = 0; i < paddingSize; i++) wPlain.WriteByte(p);
                }
            }

            int encryptedDataSize = (totalPlainTextSize / inputBlockSize) * outputBlockSize;
            int finalMessageSize = 12 + securityHeaderBytes.Length + encryptedDataSize;

            byte[] headerBytes;
            using (var msH = new MemoryStream())
            {
                var wH = new OpcUaBinaryWriter(msH);
                new SecureConversationMessageHeader { MessageType = "OPN", ChunkType = 'F', MessageSize = (uint)finalMessageSize, SecureChannelId = SecureChannelId }.Encode(wH);
                headerBytes = msH.ToArray();
            }

            if (signatureSize > 0)
            {
                byte[] dataToSign = new byte[headerBytes.Length + securityHeaderBytes.Length + (totalPlainTextSize - signatureSize)];
                Array.Copy(headerBytes, 0, dataToSign, 0, headerBytes.Length);
                Array.Copy(securityHeaderBytes, 0, dataToSign, headerBytes.Length, securityHeaderBytes.Length);
                Array.Copy(plainTextBlob, 0, dataToSign, headerBytes.Length + securityHeaderBytes.Length, totalPlainTextSize - signatureSize);
                byte[] sig = _securityPolicy.Sign(dataToSign);
                Array.Copy(sig, 0, plainTextBlob, totalPlainTextSize - signatureSize, signatureSize);
            }

            byte[] encryptedBlob = _securityPolicy.EncryptAsymmetric(plainTextBlob);
            byte[] finalPacket = new byte[headerBytes.Length + securityHeaderBytes.Length + encryptedBlob.Length];
            Array.Copy(headerBytes, 0, finalPacket, 0, headerBytes.Length);
            Array.Copy(securityHeaderBytes, 0, finalPacket, headerBytes.Length, securityHeaderBytes.Length);
            Array.Copy(encryptedBlob, 0, finalPacket, headerBytes.Length + securityHeaderBytes.Length, encryptedBlob.Length);
            return finalPacket;
        }

        private byte[] PrepareSymmetricPacket(byte[] bodyBytes, uint requestId, uint sequenceNumber, bool isClose)
        {
            int plainContentSize = 8 + bodyBytes.Length;
            int signatureSize = _securityPolicy.SymmetricSignatureSize;
            int blockSize = _securityPolicy.SymmetricBlockSize;
            int paddingSize = PaddingCalculator.CalculatePaddingSize(plainContentSize, signatureSize, blockSize);
            int totalBodySize = 4 + plainContentSize + paddingSize + signatureSize;
            int totalMessageSize = 12 + totalBodySize;

            byte[] packet = new byte[totalMessageSize];
            using (var ms = new MemoryStream(packet))
            {
                var w = new OpcUaBinaryWriter(ms);
                new SecureConversationMessageHeader { MessageType = isClose ? "CLO" : "MSG", ChunkType = 'F', MessageSize = (uint)totalMessageSize, SecureChannelId = SecureChannelId }.Encode(w);
                w.WriteUInt32(_tokenId);

                byte[] innerPart = new byte[plainContentSize + paddingSize + signatureSize];
                using (var msInner = new MemoryStream(innerPart))
                {
                    var wInner = new OpcUaBinaryWriter(msInner);
                    new SequenceHeader { SequenceNumber = sequenceNumber, RequestId = requestId }.Encode(wInner);
                    wInner.WriteBytes(bodyBytes);
                    if (paddingSize > 0)
                    {
                        byte p = (byte)(paddingSize - 1);
                        for (int i = 0; i < paddingSize; i++) wInner.WriteByte(p);
                    }
                }

                if (signatureSize > 0)
                {
                    byte[] dataToSign = new byte[16 + innerPart.Length - signatureSize];
                    Array.Copy(packet, 0, dataToSign, 0, 16);
                    Array.Copy(innerPart, 0, dataToSign, 16, innerPart.Length - signatureSize);
                    byte[] sig = _securityPolicy.SignSymmetric(dataToSign);
                    Array.Copy(sig, 0, innerPart, innerPart.Length - signatureSize, signatureSize);
                }

                if (_securityMode == MessageSecurityMode.SignAndEncrypt)
                    w.WriteBytes(_securityPolicy.EncryptSymmetric(innerPart));
                else
                    w.WriteBytes(innerPart);
            }
            return packet;
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
            int port = uri.Port == -1 ? 4840 : uri.Port;

            _stream = await CreateStreamAsync(uri.Host, port, cancellationToken);

            // 1. Handshake (Serial)
            await PerformHandshakeAsync(uri.AbsoluteUri, cancellationToken);

            // 2. Initialize Dispatcher
            _channelCts = new CancellationTokenSource();
            _isClosing = false;
            _responseProcessorTask = Task.Run(ProcessResponsesAsync, _channelCts.Token);

            // 3. Open Secure Channel
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
                RequestedSessionTimeout = _heartbeatIntervalMs * 3,
                MaxResponseMessageSize = 0
            };

            var response = await SendRequestAsync<CreateSessionRequest, CreateSessionResponse>(req, cancellationToken);

            _authenticationToken = response.AuthenticationToken ?? new NodeId(0, 0);
            _sessionServerNonce = response.ServerNonce;

            if (response.ServerCertificate != null && response.ServerCertificate.Length > 0)
            {
                _serverCertificate = X509CertificateLoader.LoadCertificate(response.ServerCertificate);
            }

            _heartbeatIntervalMs = response.RevisedSessionTimeout > 0 && response.RevisedSessionTimeout < _heartbeatIntervalMs ? (uint)(response.RevisedSessionTimeout * 0.75) : _heartbeatIntervalMs;

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

            StartHeartbeat();
        }

        private void StartHeartbeat()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts = new CancellationTokenSource();

            _heartbeatTask = Task.Run(async () =>
            {
                var token = _heartbeatCts.Token;
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay((int)_heartbeatIntervalMs, token);

                        if (_authenticationToken != null && _authenticationToken.NumericIdentifier != 0 && !_isClosing)
                        {
                            var readReq = new ReadRequest
                            {
                                RequestHeader = CreateRequestHeader(),
                                NodesToRead = [new ReadValueId(_heartbeatNodeId) { AttributeId = 13 }]
                            };
                            readReq.RequestHeader.TimeoutHint = (uint)_heartbeatTimeoutHintMs;

                            await SendRequestAsync<ReadRequest, ReadResponse>(readReq, token);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception)
                {
                    await DisconnectAsync();
                }
            }, _heartbeatCts.Token);
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

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_renewCts.Token, cancellationToken);
            var token = linkedCts.Token;

            // if lifetime is 0, set default 1 hour
            if (lifetimeMs == 0) lifetimeMs = 3600000;

            // renew after 75% of lifetime
            int delayMs = (int)(lifetimeMs * 0.75);

            _renewTask = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs, token);
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            await RenewSecureChannelAsync(token);

                            // SUCCESS
                            return;
                        }
                        catch (Exception)
                        {
                            // FAILURE
                            try
                            {
                                await Task.Delay(5000, token);
                            }
                            catch (OperationCanceledException) { break; }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception)
                {
                    await DisconnectAsync();
                }
                finally
                {
                    linkedCts.Dispose();
                }
            }, token);
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
            var request = new OpenSecureChannelRequest
            {
                RequestHeader = CreateRequestHeader(),
                ClientProtocolVersion = 0,
                RequestType = SecurityTokenRequestType.Issue,
                SecurityMode = _securityMode,
                ClientNonce = _clientNonce,
                RequestedLifetime = 3600000
            };

            var response = await SendRequestAsync<OpenSecureChannelRequest, OpenSecureChannelResponse>(request, cancellationToken);

            if (response.ResponseHeader?.ServiceResult != 0)
                throw new Exception($"OpenSecureChannel Failed: 0x{response.ResponseHeader?.ServiceResult:X8}");

            SecureChannelId = response.SecurityToken!.ChannelId;
            _tokenId = response.SecurityToken.TokenId;

            if (response.ServerNonce != null && _clientNonce != null)
            {
                _securityPolicy.DeriveKeys(_clientNonce, response.ServerNonce);
            }

            StartRenewalLoop(response.SecurityToken.RevisedLifetime, cancellationToken);
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
            bool isShutdownRequest = request is CloseSessionRequest || request is CloseSecureChannelRequest;

            if (_stream == null || (_isClosing && !isShutdownRequest))
                throw new InvalidOperationException("Channel is not connected or is closing.");

            uint handle = (uint)Interlocked.Increment(ref _requestId);
            //uint handle = (uint)_requestId;
            var pending = new PendingRequest();
            _pendingRequests.TryAdd(handle, pending);
            try
            {
                byte[] bodyBytes;
                using (var ms = new MemoryStream())
                {
                    var w = new OpcUaBinaryWriter(ms);
                    if (request is GetEndpointsRequest reqGe) reqGe.Encode(w);
                    else if (request is OpenSecureChannelRequest reqOsc) reqOsc.Encode(w);
                    else if (request is CreateSessionRequest reqCs) reqCs.Encode(w);
                    else if (request is ActivateSessionRequest reqAsr) reqAsr.Encode(w);
                    else if (request is ReadRequest reqRr) reqRr.Encode(w);
                    else if (request is WriteRequest reqWr) reqWr.Encode(w);
                    else if (request is BrowseRequest reqBr) reqBr.Encode(w);
                    else if (request is CreateSubscriptionRequest reqCsr) reqCsr.Encode(w);
                    else if (request is CreateMonitoredItemsRequest reqCmir) reqCmir.Encode(w);
                    else if (request is PublishRequest reqPr) reqPr.Encode(w);
                    else if (request is CloseSessionRequest reqCsr2) reqCsr2.Encode(w);
                    else if (request is CloseSecureChannelRequest reqCscr) reqCscr.Encode(w);
                    else if (request is CallRequest reqCr) reqCr.Encode(w);
                    else if (request is BrowseNextRequest reqBnr) reqBnr.Encode(w);
                    else if (request is DeleteMonitoredItemsRequest reqDmir) reqDmir.Encode(w);
                    else if (request is DeleteSubscriptionsRequest reqDsr) reqDsr.Encode(w);
                    else if (request is ModifyMonitoredItemsRequest reqMmir) reqMmir.Encode(w);
                    else if (request is SetMonitoringModeRequest reqSmmr) reqSmmr.Encode(w);
                    else if (request is SetPublishingModeRequest reqSpmr) reqSpmr.Encode(w);
                    else if (request is TranslateBrowsePathsToNodeIdsRequest reqTbpt) reqTbpt.Encode(w);
                    else throw new NotImplementedException($"Encoding for {typeof(TRequest).Name} not implemented.");
                    bodyBytes = ms.ToArray();
                }

                await _lock.WaitAsync(cancellationToken);
                try
                {
                    // Assign sequence number inside the lock to ensure wire order
                    uint seq = _sequenceNumber++;

                    byte[] finalPacket = request is OpenSecureChannelRequest
                        ? PrepareOpenSecureChannelPacket(bodyBytes, handle, seq)
                        : PrepareSymmetricPacket(bodyBytes, handle, seq, request is CloseSecureChannelRequest);

                    await _stream!.WriteAsync(finalPacket, cancellationToken);
                }
                finally
                {
                    _lock.Release();
                }

                if (request is CloseSecureChannelRequest)
                {
                    return new TResponse();
                }

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _channelCts!.Token);
                byte[] decryptedBytes = await pending.Tcs.Task.WaitAsync(linkedCts.Token);

                using var msRes = new MemoryStream(decryptedBytes);
                var r = new OpcUaBinaryReader(msRes);
                r.ReadUInt32(); // SequenceNumber
                r.ReadUInt32(); // RequestId
                NodeId typeId = NodeId.Decode(r);

                if (typeId.NumericIdentifier == 397)
                {
                    var fault = ResponseHeader.Decode(r);
                    throw new Exception($"Server returned ServiceFault: 0x{fault.ServiceResult:X8}");
                }

                var response = new TResponse();
                if (response is ReadResponse resRr) resRr.Decode(r);
                else if (response is WriteResponse resWr) resWr.Decode(r);
                else if (response is BrowseResponse resBr) resBr.Decode(r);
                else if (response is CreateSubscriptionResponse resCsr) resCsr.Decode(r);
                else if (response is CreateMonitoredItemsResponse resCmir) resCmir.Decode(r);
                else if (response is PublishResponse resPr) resPr.Decode(r);
                else if (response is ActivateSessionResponse resAsr) resAsr.Decode(r);
                else if (response is CreateSessionResponse resCsr2) resCsr2.Decode(r);
                else if (response is CloseSessionResponse resCsr3) resCsr3.Decode(r);
                else if (response is OpenSecureChannelResponse resOsc) resOsc.Decode(r);
                else if (response is GetEndpointsResponse resGer) resGer.Decode(r);
                else if (response is CloseSecureChannelResponse resCscr) resCscr.Decode(r);
                else if (response is CallResponse resCr) resCr.Decode(r);
                else if (response is BrowseNextResponse resBnr) resBnr.Decode(r);
                else if (response is DeleteMonitoredItemsResponse resDmir) resDmir.Decode(r);
                else if (response is DeleteSubscriptionsResponse resDsr) resDsr.Decode(r);
                else if (response is ModifyMonitoredItemsResponse resMmir) resMmir.Decode(r);
                else if (response is SetMonitoringModeResponse resSmmr) resSmmr.Decode(r);
                else if (response is SetPublishingModeResponse resSpmr) resSpmr.Decode(r);
                else if (response is TranslateBrowsePathsToNodeIdsResponse resTbpt) resTbpt.Decode(r);
                else throw new NotImplementedException($"Decoding for {typeof(TResponse).Name} not implemented.");

                return response;
            }
            catch
            {
                _pendingRequests.TryRemove(handle, out _);
                throw;
            }
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

        private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int count, CancellationToken cancellationToken)
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

        protected virtual async Task<Stream> CreateStreamAsync(string host, int port, CancellationToken ct)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port, ct);
            return _client.GetStream();
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();

            var cts = _renewCts;
            _renewCts = null;
            cts?.Dispose();

            _lock.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            try
            {
                DisconnectAsync().GetAwaiter().GetResult();
            }
            catch { }

            var cts = _renewCts;
            _renewCts = null;
            cts?.Dispose();

            _stream?.Dispose();
            _client?.Dispose();
            _lock.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}