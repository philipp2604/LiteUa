using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using LiteUa.Stack.Subscription.MonitoredItem;
using LiteUa.Transport;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a Subscription in the OPC UA protocol.
    /// </summary>
    /// <param name="channel">The underlying <see cref="IUaTcpClientChannel"/> to use for communication.</param>
    public class Subscription(IUaTcpClientChannel channel) : IAsyncDisposable, IDisposable
    {
        private readonly IUaTcpClientChannel _channel = channel;
        private uint _subscriptionId;
        private CancellationTokenSource? _cts;
        private Task? _publishTask;
        private double _publishingInterval;
        private uint _keepAliveCount;

        private readonly Queue<SubscriptionAcknowledgement> _pendingAcks = new();
        private readonly Lock _ackLock = new();

        /// <summary>
        /// A callback invoked when a monitored item's data changes.
        /// </summary>
        public event Action<uint, DataValue>? DataChanged;

        /// <summary>
        /// A callback invoked when the connection is lost.
        /// </summary>
        public event Action<Exception>? ConnectionLost;

        /// <summary>
        /// Creates multiple monitored items asynchronously.
        /// </summary>
        /// <param name="nodeIds">The node ids of the items to monitor.</param>
        /// <param name="clientHandles">The client handles for the MonitoredItems.</param>
        /// <returns>The ids of the monitored items.</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<uint[]> CreateMonitoredItemsAsync(NodeId[] nodeIds, uint[] clientHandles)
        {
            if (nodeIds.Length != clientHandles.Length) throw new ArgumentException("Count mismatch");

            var items = new MonitoredItemCreateRequest[nodeIds.Length];
            for (int i = 0; i < nodeIds.Length; i++)
            {
                items[i] = new MonitoredItemCreateRequest(
                    new ReadValueId(nodeIds[i]),
                    2, // Reporting
                    new MonitoringParameters()
                    {
                        ClientHandle = clientHandles[i],
                        SamplingInterval = -1, // subscription default
                        QueueSize = 1
                    }
                );
            }

            var req = new CreateMonitoredItemsRequest
            {
                RequestHeader = _channel.CreateRequestHeader(),
                SubscriptionId = _subscriptionId,
                ItemsToCreate = items,
                TimestampsToReturn = TimestampsToReturn.Both
            };

            var res = await _channel.SendRequestAsync<CreateMonitoredItemsRequest, CreateMonitoredItemsResponse>(req);

            if (res.Results != null)
            {
            }

            var results = new uint[res.Results?.Length ?? 0];
            for (int i = 0; i < (res.Results?.Length ?? -1); i++)
            {
                if (res.Results![i].StatusCode.Code != 0)
                {
                    // return 0 if failed
                    results[i] = 0;
                }
                else
                {
                    results[i] = res.Results[i].MonitoredItemId;
                }
            }
            return results;
        }

        /// <summary>
        /// Creates the subscription asynchronously.
        /// </summary>
        /// <param name="publishingInterval">The publishing interval, default is 1000ms.</param>
        /// <returns>A task for monitoring the asynchronous operation.</returns>
        public async Task CreateAsync(double publishingInterval = 1000.0)
        {
            var req = new CreateSubscriptionRequest
            {
                RequestHeader = _channel.CreateRequestHeader(),
                RequestedPublishingInterval = publishingInterval,
                RequestedLifetimeCount = 60, // KeepAlive * 6
                RequestedMaxKeepAliveCount = 10, // KeepAlive every 10 intervals
                MaxNotificationsPerPublish = 0,
                PublishingEnabled = true,
                Priority = 0
            };

            var response = await _channel.SendRequestAsync<CreateSubscriptionRequest, CreateSubscriptionResponse>(req);
            _subscriptionId = response.SubscriptionId;

            _publishingInterval = response.RevisedPublishingInterval;
            _keepAliveCount = response.RevisedMaxKeepAliveCount;

            _cts = new CancellationTokenSource();
            _publishTask = Task.Run(PublishLoop);
        }

        /// <summary>
        /// Creates a monitored item asynchronously.
        /// </summary>
        /// <param name="nodeId">The node id of the item to monitor.</param>
        /// <param name="clientHandle">The client handle.</param>
        /// <returns>The Id of the monitored item.</returns>
        /// <exception cref="Exception"></exception>
        public async Task<uint> CreateMonitoredItemAsync(NodeId nodeId, uint clientHandle)
        {
            var req = new CreateMonitoredItemsRequest
            {
                RequestHeader = _channel.CreateRequestHeader(),
                SubscriptionId = _subscriptionId,
                ItemsToCreate =
                [
                    new MonitoredItemCreateRequest(
                        new ReadValueId(nodeId),
                        2, // Reporting
                        new MonitoringParameters()
                        {
                            ClientHandle = clientHandle,
                            SamplingInterval = 500,
                            QueueSize = 1
                        })
                ]
            };

            var res = await _channel.SendRequestAsync<CreateMonitoredItemsRequest, CreateMonitoredItemsResponse>(req);

            // Check result codes
            if (res.Results?[0]?.StatusCode.Code != 0)
                throw new Exception($"CreateMonitoredItem failed: {res.Results?[0]?.StatusCode}");

            return res.Results[0].MonitoredItemId;
        }

        private async Task PublishLoop()
        {
            while (!_cts!.IsCancellationRequested)
            {
                try
                {
                    // 1. Prepare Acks
                    SubscriptionAcknowledgement[] acksToSend;
                    lock (_ackLock)
                    {
                        acksToSend = [.. _pendingAcks];
                    }

                    var req = new PublishRequest
                    {
                        RequestHeader = _channel.CreateRequestHeader(),
                        SubscriptionAcknowledgements = acksToSend
                    };

                    // calculate TimeoutHint
                    // Latest answer after (Interval * KeepAlive)
                    // We give him +50% margin or min 5 seconds.
                    double maxSilenceMs = _publishingInterval * _keepAliveCount * 1.5;
                    if (maxSilenceMs < 5000) maxSilenceMs = 5000;

                    req.RequestHeader.TimeoutHint = (uint)maxSilenceMs;

                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(maxSilenceMs));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);
                    try
                    {
                        // 2. Send
                        var response = await _channel.SendRequestAsync<PublishRequest, PublishResponse>(req);

                        if (response.NotificationMessage == null)
                        {
                            throw new Exception("PublishResponse contains no NotificationMessage.");
                        }

                        // 3. Remove acks, prepare new ones
                        lock (_ackLock)
                        {
                            for (int i = 0; i < acksToSend.Length; i++) _pendingAcks.Dequeue();

                            // prepare new acks
                            _pendingAcks.Enqueue(new SubscriptionAcknowledgement
                            {
                                SubscriptionId = response.SubscriptionId,
                                SequenceNumber = response.NotificationMessage.SequenceNumber
                            });
                        }

                        // 4. Handle Notifications
                        if (response.NotificationMessage.NotificationData != null)
                        {
                            foreach (var extObj in response.NotificationMessage.NotificationData)
                            {
                                // DataChangeNotification (811)
                                if (extObj.TypeId.NumericIdentifier == 811 && extObj.Encoding == 0x01)
                                {
                                    using var ms = new System.IO.MemoryStream(extObj.Body ?? throw new Exception("Body of DataChangeNotification is null."));
                                    var r = new OpcUaBinaryReader(ms);
                                    var dcn = DataChangeNotification.Decode(r);
                                    if (dcn.MonitoredItems != null)
                                    {
                                        foreach (var item in dcn.MonitoredItems)
                                        {
                                            if (item?.Value != null)
                                            {
                                                DataChanged?.Invoke(item.ClientHandle, item.Value);
                                            }
                                        }
                                    }
                                }
                                // StatusChangeNotification 820
                                else if (extObj.TypeId.NumericIdentifier == 820 && extObj.Encoding == 0x01)
                                {
                                    if (extObj.Body != null)
                                    {
                                        using var ms = new System.IO.MemoryStream(extObj.Body);
                                        var r = new OpcUaBinaryReader(ms);
                                        var scn = StatusChangeNotification.Decode(r);
                                        if (scn.Status.IsBad)
                                        {
                                            ConnectionLost?.Invoke(new Exception($"Subscription terminated by Server: {scn.Status}"));
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (_cts.IsCancellationRequested) return;
                        throw new TimeoutException($"Publish Request timed out after {maxSilenceMs} ms (No KeepAlive received).");
                    }
                }
                catch (Exception ex)
                {
                    if (_cts.IsCancellationRequested) return;
                    ConnectionLost?.Invoke(ex);
                    return;
                }
            }
        }

        /// <summary>
        /// Deletes the subscription asynchronously from the server.
        /// </summary>
        /// <returns>A task to monitor the asynchronous operation.</returns>
        public async Task DeleteAsync()
        {
            // 1. Stop loop
            Stop();

            // 2. Delete subscription
            if (_subscriptionId != 0)
            {
                await _channel.DeleteSubscriptionsAsync([_subscriptionId]);
                _subscriptionId = 0;
            }
        }

        /// <summary>
        /// Deletes monitored items asynchronously from the subscription.
        /// </summary>
        /// <param name="monitoredItemIds">The ids of the monitored items to delete.</param>
        /// <returns>A task to monitor the asynchronous operation.</returns>
        public async Task DeleteMonitoredItemsAsync(uint[] monitoredItemIds)
        {
            try
            {
                await _channel.DeleteMonitoredItemsAsync(_subscriptionId, monitoredItemIds);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            Stop();
            await _channel.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}