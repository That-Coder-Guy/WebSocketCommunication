namespace WebSocketCommunication
{
    /// <summary>
    /// Provides data for the event that is raised when a WebSocket connection attempt fails.
    /// </summary>
    public class ConnectionFailedEventArgs
    {
        /// <summary>
        /// Gets the error code that indicates the reason for the connection failure.
        /// </summary>
        public WebSocketError Error { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionFailedEventArgs"/> class with the specified error.
        /// </summary>
        /// <param name="error">The error code representing the connection failure reason.</param>
        public ConnectionFailedEventArgs(WebSocketError error)
        {
            Error = error;
        }
    }
}
