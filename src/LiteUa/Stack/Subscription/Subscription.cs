using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using LiteUa.Stack.Subscription.MonitoredItem;
using LiteUa.Transport;
using System.Collections.Concurrent;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a Subscription in the OPC UA protocol.
    /// </summary>
    /// <param name="channel">The underlying <see cref="IUaTcpClientChannel"/> to use for communication.</param>
    public class Subscription(IUaTcpClientChannel channel, uint maxPublishRequests, double publishTimeoutMultiplier, uint minPublishTimeoutMs) : IAsyncDisposable, IDisposable
    {
        private readonly IUaTcpClientChannel _channel = channel;
        private uint _subscriptionId;
        private CancellationTokenSource? _cts;
        private Task? _publishTask;
        private double _publishingInterval;
        private uint _keepAliveCount;
        private readonly uint _maxPublishRequests = maxPublishRequests;
        private readonly double _publishTimeoutMultiplier = publishTimeoutMultiplier;
        private readonly uint _minPublishTimeoutMs = minPublishTimeoutMs;
        private readonly ConcurrentDictionary<uint, SubscriptionAcknowledgement> _unacknowledgedSequences = new();

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
            var outstandingTasks = new List<Task>();

            while (!_cts!.IsCancellationRequested)
            {
                try
                {
                    // Keep the pipeline full
                    while (outstandingTasks.Count < _maxPublishRequests && !_cts.IsCancellationRequested)
                    {
                        outstandingTasks.Add(SendPublishRequestInternalAsync());
                    }

                    // Wait for any one request to return
                    var completedTask = await Task.WhenAny(outstandingTasks);
                    outstandingTasks.Remove(completedTask);

                    // Await to propagate potential exceptions
                    await completedTask;
                }
                catch (Exception ex)
                {
                    if (_cts.IsCancellationRequested) return;
                    ConnectionLost?.Invoke(ex);
                    return;
                }
            }
        }

        private async Task SendPublishRequestInternalAsync()
        {
            SubscriptionAcknowledgement[] acksInThisRequest = _unacknowledgedSequences.Values.ToArray();

            var req = new PublishRequest
            {
                RequestHeader = _channel.CreateRequestHeader(),
                SubscriptionAcknowledgements = acksInThisRequest
            };

            double calculatedTimeout = _publishingInterval * _keepAliveCount * _publishTimeoutMultiplier;
            req.RequestHeader.TimeoutHint = (uint)Math.Max(calculatedTimeout, _minPublishTimeoutMs);

            var response = await _channel.SendRequestAsync<PublishRequest, PublishResponse>(req, _cts!.Token);

            if (response.Results != null)
            {
                for (int i = 0; i < response.Results.Length; i++)
                {
                    if (response.Results[i].IsGood)
                    {
                        // Server confirmed receipt, we can stop sending this sequence number
                        _unacknowledgedSequences.TryRemove(acksInThisRequest[i].SequenceNumber, out _);
                    }
                }
            }

            if (response.NotificationMessage != null)
            {
                var newAck = new SubscriptionAcknowledgement
                {
                    SubscriptionId = response.SubscriptionId,
                    SequenceNumber = response.NotificationMessage.SequenceNumber
                };
                _unacknowledgedSequences.TryAdd(newAck.SequenceNumber, newAck);

                // 6. Process the payload
                if (response.NotificationMessage.NotificationData != null)
                {
                    HandleNotificationData(response.NotificationMessage.NotificationData);
                }
            }
        }

        private void HandleNotificationData(ExtensionObject[] notificationData)
        {
            foreach (var extObj in notificationData)
            {
                // DataChangeNotification (TypeId 811)
                if (extObj.TypeId.NumericIdentifier == 811 && extObj.Body != null)
                {
                    using var ms = new MemoryStream(extObj.Body);
                    var dcn = DataChangeNotification.Decode(new OpcUaBinaryReader(ms));
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
                // StatusChangeNotification (TypeId 820)
                else if (extObj.TypeId.NumericIdentifier == 820 && extObj.Body != null)
                {
                    using var ms = new MemoryStream(extObj.Body);
                    var scn = StatusChangeNotification.Decode(new OpcUaBinaryReader(ms));
                    if (scn.Status.IsBad)
                    {
                        ConnectionLost?.Invoke(new Exception($"Server reported Bad status for subscription: {scn.Status}"));
                    }
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