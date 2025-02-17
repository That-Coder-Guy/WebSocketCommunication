namespace WebSocketCommunication
{
    /// <summary>
    /// Provides data for the event that is raised when a WebSocket connection is closed.
    /// </summary>
    public class DisconnectEventArgs
    {
        /// <summary>
        /// Gets the reason why the WebSocket connection was closed.
        /// </summary>
        public WebSocketClosureReason Reason { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectEventArgs"/> class with the specified closure reason.
        /// </summary>
        /// <param name="reason">The reason for the WebSocket connection closure.</param>
        public DisconnectEventArgs(WebSocketClosureReason reason)
        {
            Reason = reason;
        }
    }
}
