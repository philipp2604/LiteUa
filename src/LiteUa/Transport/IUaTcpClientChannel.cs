using LiteUa.BuiltIn;
using LiteUa.Stack.Discovery;
using LiteUa.Stack.Session.Identity;
using LiteUa.Stack.Subscription.MonitoredItem;
using LiteUa.Stack.View;
using LiteUa.Transport.Headers;

namespace LiteUa.Transport
{
    /// <summary>
    /// An interface representing a UA TCP client channel for communication with an OPC UA server.
    /// </summary>
    public interface IUaTcpClientChannel : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets the size of the send buffer used for communication.
        /// </summary>
        public uint SendBufferSize { get; }

        /// <summary>
        /// Gets the size of the receive buffer used for communication.
        /// </summary>
        public uint ReceiveBufferSize { get; }

        /// <summary>
        /// Gets the maximum message size supported by the channel.
        /// </summary>
        public uint MaxMessageSize { get; }

        /// <summary>
        /// Gets the maximum chunk count supported by the channel.
        /// </summary>
        public uint MaxChunkCount { get; }

        /// <summary>
        /// Resolves an array of relative paths to their corresponding NodeIds starting from a specified start node.
        /// </summary>
        /// <param name="startNode">The starting node.</param>
        /// <param name="paths">An array of relative paths.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> encapsulating the resolved node ids.</returns>
        public Task<NodeId?[]> ResolveNodeIdsAsync(NodeId startNode, string[] paths, CancellationToken token = default);

        /// <summary>
        /// Modifies the parameters of existing monitored items within a subscription.
        /// </summary>
        /// <param name="subscriptionId">The server-assigned identifier for the subscription.</param>
        /// <param name="monitoredItemIds">The server-assigned identifiers for the monitored items to modify.</param>
        /// <param name="clientHandles">The client-assigned identifiers for the monitored items.</param>
        /// <param name="samplingInterval">The requested sampling interval in milliseconds.</param>
        /// <param name="queueSize">The requested size of the monitored item queue.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> containing the results for each monitored item modification.</returns>
        public Task<MonitoredItemModifyResult[]?> ModifyMonitoredItemsAsync(uint subscriptionId, uint[] monitoredItemIds, uint[] clientHandles, double samplingInterval, uint queueSize, CancellationToken token = default);

        /// <summary>
        /// Browses a set of nodes to discover their references and related nodes.
        /// </summary>
        /// <param name="nodesToBrowse">The array of NodeIds to browse.</param>
        /// <param name="maxRefs">The maximum number of references to return per node (0 for no limit).</param>
        /// <param name="token">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> containing an array of reference descriptions for each input node.</returns>
        public Task<ReferenceDescription[][]> BrowseAsync(NodeId[] nodesToBrowse, uint maxRefs = 0, CancellationToken token = default);

        /// <summary>
        /// Sets the monitoring mode (Disabled, Sampling, or Reporting) for one or more monitored items.
        /// </summary>
        /// <param name="subscriptionId">The server-assigned identifier for the subscription.</param>
        /// <param name="monitoredItemIds">The identifiers of the items to update.</param>
        /// <param name="monitoringMode">The new monitoring mode (0: Disabled, 1: Sampling, 2: Reporting).</param>
        /// <param name="token">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> containing the status codes for each item.</returns>
        public Task<StatusCode[]?> SetMonitoringModeAsync(uint subscriptionId, uint[] monitoredItemIds, uint monitoringMode, CancellationToken token = default);

        /// <summary>
        /// Enables or disables the publishing of notifications for one or more subscriptions.
        /// </summary>
        /// <param name="subscriptionIds">The identifiers of the subscriptions to update.</param>
        /// <param name="publishingEnabled">True to enable publishing, false to disable it.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> containing the status codes for each subscription.</returns>
        public Task<StatusCode[]?> SetPublishingModeAsync(uint[] subscriptionIds, bool publishingEnabled, CancellationToken token = default);

        /// <summary>
        /// Disconnects from the OPC UA server and cleans up the session and secure channel.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task DisconnectAsync();

        /// <summary>
        /// Creates a standard request header used for OPC UA service calls.
        /// </summary>
        /// <returns>A <see cref="RequestHeader"/> initialized with current timestamps and request handles.</returns>
        public RequestHeader CreateRequestHeader();

        /// <summary>
        /// Establishes a secure channel and physical connection with the OPC UA server.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new session on the connected OPC UA server.
        /// </summary>
        /// <param name="sessionName">The descriptive name for the session.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task CreateSessionAsync(string sessionName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Activates the session using the specified user identity credentials.
        /// </summary>
        /// <param name="identity">The user identity (Anonymous, UserName, or Certificate).</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task ActivateSessionAsync(IUserIdentity identity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the current values and attributes from the specified nodes.
        /// </summary>
        /// <param name="nodesToRead">The array of NodeIds to read.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> containing the data values and status for each node.</returns>
        public Task<DataValue[]?> ReadAsync(NodeId[] nodesToRead, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes values to the specified nodes in the OPC UA server.
        /// </summary>
        /// <param name="nodes">The NodeIds of the variables to write to.</param>
        /// <param name="values">The data values to be written.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> containing the status codes for each write operation.</returns>
        public Task<StatusCode[]?> WriteAsync(NodeId[] nodes, DataValue[] values, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calls a method on an object in the OPC UA server.
        /// </summary>
        /// <param name="objectId">The NodeId of the object that owns the method.</param>
        /// <param name="methodId">The NodeId of the method to call.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <param name="inputArguments">The input parameters for the method.</param>
        /// <returns>A <see cref="Task"/> containing the output arguments returned by the method.</returns>
        public Task<Variant[]> CallAsync(NodeId objectId, NodeId methodId, CancellationToken cancellationToken = default, params Variant[] inputArguments);

        /// <summary>
        /// Sends a raw OPC UA request to the server.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request (e.g., ReadRequest).</typeparam>
        /// <typeparam name="TResponse">The type of the expected response (e.g., ReadResponse).</typeparam>
        /// <param name="request">The request object to send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> containing the server's response.</returns>
        public Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TResponse : new();

        /// <summary>
        /// Deletes specific monitored items from a subscription.
        /// </summary>
        /// <param name="subscriptionId">The server-assigned identifier for the subscription.</param>
        /// <param name="monitoredItemIds">The identifiers of the items to be deleted.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> containing the status codes for each deletion.</returns>
        public Task<StatusCode[]?> DeleteMonitoredItemsAsync(uint subscriptionId, uint[] monitoredItemIds, CancellationToken token = default);

        /// <summary>
        /// Deletes one or more subscriptions from the OPC UA server.
        /// </summary>
        /// <param name="subscriptionIds">The identifiers of the subscriptions to delete.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> containing the status codes for each subscription deletion.</returns>
        public Task<StatusCode[]?> DeleteSubscriptionsAsync(uint[] subscriptionIds, CancellationToken token = default);

        /// <summary>
        /// Gets a list of endpoints supported by the server and their security configurations.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> containing the GetEndpointsResponse.</returns>
        public Task<GetEndpointsResponse> GetEndpointsAsync(CancellationToken cancellationToken = default);
    }
}