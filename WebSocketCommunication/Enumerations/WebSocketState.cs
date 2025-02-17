namespace WebSocketCommunication
{
    /// <summary>
    /// Represents the different states of a WebSocket connection.
    /// </summary>
    public enum WebSocketState
    {
        /// <summary>
        /// Reserved for future use. The WebSocket state is undefined.
        /// </summary>
        None = 0,

        /// <summary>
        /// The connection is negotiating the handshake with the remote endpoint.
        /// </summary>
        Connecting = 1,

        /// <summary>
        /// The WebSocket connection is open and ready to send/receive messages.
        /// </summary>
        Open = 2,

        /// <summary>
        /// A close message has been sent to the remote endpoint, awaiting acknowledgment.
        /// </summary>
        CloseSent = 3,

        /// <summary>
        /// A close message has been received from the remote endpoint, but the connection is not fully closed yet.
        /// </summary>
        CloseReceived = 4,

        /// <summary>
        /// The WebSocket connection has been closed gracefully following the close handshake.
        /// </summary>
        Closed = 5,

        /// <summary>
        /// The WebSocket connection was terminated abruptly without completing the close handshake.
        /// </summary>
        Aborted = 6
    }
}
