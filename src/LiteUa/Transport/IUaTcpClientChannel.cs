using LiteUa.BuiltIn;
using LiteUa.Client;
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteUa.Transport
{
    public interface IUaTcpClientChannel : IDisposable, IAsyncDisposable
    {
        public uint SendBufferSize { get; }

        public uint ReceiveBufferSize { get; }
        public uint MaxMessageSize { get; }
        public uint MaxChunkCount { get; }

        public Task<NodeId?[]> ResolveNodeIdsAsync(NodeId startNode, string[] paths, CancellationToken token = default);

        public Task<MonitoredItemModifyResult[]?> ModifyMonitoredItemsAsync(uint subscriptionId, uint[] monitoredItemIds, uint[] clientHandles, double samplingInterval, uint queueSize, CancellationToken token = default);

        public Task<ReferenceDescription[][]> BrowseAsync(NodeId[] nodesToBrowse, uint maxRefs = 0, CancellationToken token = default);

        public Task<StatusCode[]?> SetMonitoringModeAsync(uint subscriptionId, uint[] monitoredItemIds, uint monitoringMode, CancellationToken token = default);

        public Task<StatusCode[]?> SetPublishingModeAsync(uint[] subscriptionIds, bool publishingEnabled, CancellationToken token = default);

        public Task DisconnectAsync();

        public RequestHeader CreateRequestHeader();

        public Task ConnectAsync(CancellationToken cancellationToken = default);

        public Task CreateSessionAsync(string sessionName, CancellationToken cancellationToken = default);

        public Task ActivateSessionAsync(IUserIdentity identity, CancellationToken cancellationToken = default);

        public Task<DataValue[]?> ReadAsync(NodeId[] nodesToRead, CancellationToken cancellationToken = default);

        public Task<StatusCode[]?> WriteAsync(NodeId[] nodes, DataValue[] values, CancellationToken cancellationToken = default);

        public Task<Variant[]> CallAsync(NodeId objectId, NodeId methodId, CancellationToken cancellationToken = default, params Variant[] inputArguments);

        public Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TResponse : new();

        public Task<StatusCode[]?> DeleteMonitoredItemsAsync(uint subscriptionId, uint[] monitoredItemIds, CancellationToken token = default);

        public Task<StatusCode[]?> DeleteSubscriptionsAsync(uint[] subscriptionIds, CancellationToken token = default);

        public Task<GetEndpointsResponse> GetEndpointsAsync(CancellationToken cancellationToken = default);
    }
}
