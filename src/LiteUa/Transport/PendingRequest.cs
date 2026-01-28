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
    }
}