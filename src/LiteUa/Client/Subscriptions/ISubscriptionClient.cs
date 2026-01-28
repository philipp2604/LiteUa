using LiteUa.BuiltIn;

namespace LiteUa.Client.Subscriptions
{
    public interface ISubscriptionClient : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// A callback that is invoked when data changes for a subscribed node.
        /// </summary>
        public event Action<uint, DataValue>? DataChanged;

        /// <summary>
        /// A callback that is invoked when the connection status changes.
        /// </summary>
        public event Action<bool>? ConnectionStatusChanged;

        /// <summary>
        /// Starts the subscription client, initiating the connection and subscription management loop.
        /// </summary>
        public void Start();

        /// <summary>
        /// Subscribes to data changes for the specified node IDs with the given publishing interval.
        /// </summary>
        /// <param name="nodeIds">The node ids to subscribe to.</param>
        /// <param name="publishingInterval">The publishing interval.</param>
        /// <returns>A task encapsulating the returned handles.</returns>
        public Task<uint[]> SubscribeAsync(NodeId[] nodeIds, double publishingInterval = 1000.0);
    }
}