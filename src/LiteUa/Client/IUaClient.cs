using LiteUa.BuiltIn;
using LiteUa.Client.Building;
using LiteUa.Security.Policies;
using LiteUa.Stack.Session.Identity;
using LiteUa.Stack.View;
using LiteUa.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Client
{
    public interface IUaClient : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// A callback that is invoked when the connection status changes.
        /// Returns true if connected, false if disconnected.
        /// </summary>
        public event Action<bool>? ConnectionStatusChanged;

        /// <summary>
        /// Builder entry point for creating an UaClient.
        /// </summary>
        /// <returns>A <see cref="UaClientBuilder"/>.</returns>
        public static UaClientBuilder Create() => new();

        /// <summary>
        /// Connects the client to the server, performing discovery, security setup, and session establishment.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the async operations.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the specified nodes asynchronously.
        /// </summary>
        /// <param name="nodeIds">The nodes to read.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the async operations.</param>
        /// <returns>A task encapsulating the read DataValues.</returns>
        public Task<DataValue[]?> ReadNodesAsync(NodeId[] nodeIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes the specified values to the specified nodes asynchronously.
        /// </summary>
        /// <param name="nodeIds">The nodes to write to.</param>
        /// <param name="values">The values to write to the nodes.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operations.</param>
        /// <returns>A task encapsulating the returned StatusCodes.</returns>
        public Task<StatusCode[]?> WriteNodesAsync(NodeId[] nodeIds, DataValue[] values, CancellationToken cancellationToken = default);

        /// <summary>
        /// Browses the specified nodes asynchronously.
        /// </summary>
        /// <param name="nodeIds">The nodes to browse.</param>
        /// <param name="maxRefs">Maxmium reference descriptions to return.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operations.</param>
        /// <returns>A task encapsulating the returned ReferenceDescriptions.</returns>
        public Task<ReferenceDescription[][]?> BrowseNodesAsync(NodeId[] nodeIds, uint maxRefs = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calls a method on the server with typed input and output arguments.
        /// </summary>
        /// <typeparam name="TInput">Input arguments type.</typeparam>
        /// <typeparam name="TOutput">Output type.</typeparam>
        /// <param name="objectId">Node Id of the method object.</param>
        /// <param name="methodId">Node Id of the method.</param>
        /// <param name="inputArgs">Input arguments of type <typeparamref name="TInput"/>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operations.</param>
        /// <returns>A task encapsulating the method's output of type <typeparamref name="TOutput"/></returns>
        public Task<TOutput> CallMethodAsync<TInput, TOutput>(NodeId objectId, NodeId methodId, TInput inputArgs, CancellationToken cancellationToken = default)
            where TInput : class
            where TOutput : class, new();

        /// <summary>
        /// Calls a method on the server with untyped input and output arguments.
        /// </summary>
        /// <param name="objectId">Node Id of the method object.</param>
        /// <param name="methodId">Node Id of the method.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operations.</param>
        /// <param name="inputArguments">Input arguments for the method call.</param>
        /// <returns>A task encapsulating the method's output as <see cref="Variant"/> array.</returns>
        public Task<Variant[]> CallMethodAsync(NodeId objectId, NodeId methodId, CancellationToken cancellationToken = default, params Variant[] inputArguments);

        /// <summary>
        /// Subscribes to data changes for the specified node IDs with a single callback.
        /// </summary>
        /// <param name="nodeIds">NodeIds to subscribe to.</param>
        /// <param name="callback">Callback to call when a data change occured.</param>
        /// <param name="interval">Sampling interval.</param>
        /// <returns>A task encapsulating an array of the subscription handles.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Task<uint[]> SubscribeAsync(NodeId[] nodeIds, Action<uint, DataValue> callback, double interval = 1000.0);

        // multiple node Ids, multiple callbacks
        /// <summary>
        /// Subscribes to data changes for the specified node IDs with individual callbacks.
        /// </summary>
        /// <param name="nodeIds">NodeIds to subscribe to.</param>
        /// <param name="callbacks">Caöllbacks for every NodeId that are being called after a data change.</param>
        /// <param name="interval">Sampling interval.</param>
        /// <returns>A task encapsulating an array of the subscription handles.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Task<uint[]> SubscribeAsync(NodeId[] nodeIds, Action<uint, DataValue>[] callbacks, double interval = 1000.0);
    }
}
