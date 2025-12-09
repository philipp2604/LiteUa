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
    public class Subscription
    {
        private readonly UaTcpClientChannel _channel;
        private uint _subscriptionId;
        private CancellationTokenSource? _cts;
        private Task? _publishTask;

        private readonly Queue<SubscriptionAcknowledgement> _pendingAcks = new Queue<SubscriptionAcknowledgement>();
        private readonly object _ackLock = new object();

        public event Action<uint, DataValue>? DataChanged;

        public Subscription(UaTcpClientChannel channel)
        {
            _channel = channel;
        }

        public async Task CreateAsync(double publishingInterval = 1000.0)
        {
            var req = new CreateSubscriptionRequest
            {
                RequestHeader = _channel.CreateRequestHeader(),
                RequestedPublishingInterval = publishingInterval,
                RequestedLifetimeCount = 60, // KeepAlive * 3
                RequestedMaxKeepAliveCount = 20,
                MaxNotificationsPerPublish = 0,
                PublishingEnabled = true,
                Priority = 0
            };

            var response = await _channel.SendRequestAsync<CreateSubscriptionRequest, CreateSubscriptionResponse>(req);
            _subscriptionId = response.SubscriptionId;

            _cts = new CancellationTokenSource();
            _publishTask = Task.Run(PublishLoop);
        }

        public async Task<uint> CreateMonitoredItemAsync(NodeId nodeId, uint clientHandle)
        {
            var req = new CreateMonitoredItemsRequest
            {
                RequestHeader = _channel.CreateRequestHeader(),
                SubscriptionId = _subscriptionId,
                ItemsToCreate = new[]
                {
                    new MonitoredItemCreateRequest(
                        new ReadValueId(nodeId),
                        2, // Reporting
                        new MonitoringParameters()
                        {
                            ClientHandle = clientHandle,
                            SamplingInterval = 500,
                            QueueSize = 1
                        })
                }
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
                        acksToSend = _pendingAcks.ToArray();
                        _pendingAcks.Clear();  /// TODO: Only remove ACKs after PublishResponse was received successfully.
                    }

                    var req = new PublishRequest
                    {
                        RequestHeader = _channel.CreateRequestHeader(),
                        SubscriptionAcknowledgements = acksToSend
                    };


                    req.RequestHeader.TimeoutHint = 60000;

                    // 2. Send
                    var response = await _channel.SendRequestAsync<PublishRequest, PublishResponse>(req);

                    if(response.NotificationMessage == null)
                    {
                        throw new Exception("PublishResponse contains no NotificationMessage.");
                    }

                    // 3. Save Ack to queue

                    lock (_ackLock)
                    {
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
                                using (var ms = new System.IO.MemoryStream(extObj.Body ?? throw new Exception("Body of DataChangeNotification is null.")))
                                {
                                    var r = new OpcUaBinaryReader(ms);
                                    var dcn = DataChangeNotification.Decode(r);
                                    if (dcn.MonitoredItems != null)
                                    {
                                        foreach (var item in dcn.MonitoredItems)
                                        {
                                            if(item?.Value != null)
                                            {
                                                DataChanged?.Invoke(item.ClientHandle, item.Value);
                                            }
                                        }
                                    }
                                }
                            }
                            /// TODO: EventNotificationList (916) or StatusChangeNotification (820) can be handled here as well.
                        }
                    }
                }
                catch (Exception ex)
                {
                    // No crash, retry.
                    if (!_cts.IsCancellationRequested)
                    {
                        Console.WriteLine($"Publish Loop Error: {ex.Message}");
                        await Task.Delay(2000);
                    }
                }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            /// TODO: Send DeleteMonitoredItems / DeleteSubscription Requests
        }
    }
}
