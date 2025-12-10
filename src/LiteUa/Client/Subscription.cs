using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Attribute;
using LiteUa.Stack.Subscription;
using LiteUa.Stack.Subscription.MonitoredItem;
using LiteUa.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Client
{
    public class Subscription(UaTcpClientChannel channel) : IAsyncDisposable, IDisposable
    {
        private readonly UaTcpClientChannel _channel = channel;
        private uint _subscriptionId;
        private CancellationTokenSource? _cts;
        private Task? _publishTask;
        private double _publishingInterval;
        private uint _keepAliveCount;

        private readonly Queue<SubscriptionAcknowledgement> _pendingAcks = new();
        private readonly Lock _ackLock = new();

        public event Action<uint, DataValue>? DataChanged;
        public event Action<Exception>? ConnectionLost;

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
                                /// TODO: EventNotificationList (916) or StatusChangeNotification (820) can be handled here as well.
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

        public void Stop()
        {
            _cts?.Cancel();
        }

        public void Dispose()
        {
            DeleteAsync().Wait();
            _channel?.DisconnectAsync().Wait();
            _channel?.Dispose();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DeleteAsync();

            if (_channel != null)
                await _channel.DisposeAsync();
            Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
