namespace LiteUa.Transport
{
    /// <summary>
    /// Represents a pending request in the transport layer.
    /// </summary>
    internal class PendingRequest
    {
        /// <summary>
        /// Gets the TaskCompletionSource for the pending request.
        /// </summary>
        public TaskCompletionSource<byte[]> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Gets or sets the expected type of the response.
        /// </summary>
        public string ExpectedType { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the expiry time of the request.
        /// </summary>
        public DateTime ExpiryTime { get; set; }
    }
}