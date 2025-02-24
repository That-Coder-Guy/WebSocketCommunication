namespace WebSocketCommunication
{
    /// <summary>
    /// Specifies possible error codes for WebSocket communication failures.
    /// </summary>
    public enum WebSocketError
    {
        #region New WebSocket Errors
        /// <summary>
        /// Indicates that there was no error.
        /// </summary>
        Success = -2,

        /// <summary>
        /// Indicates that a WebSocket operation timed out.
        /// </summary>
        Timeout = -1,
        #endregion

        #region Original WebSocket Errors
        /// <summary>
        /// Indicates that there was no native error information for the exception.
        /// </summary>
        UnknownError = 0,

        /// <summary>
        /// Indicates that a WebSocket frame with an unknown opcode was received.
        /// </summary>
        InvalidMessageType = 1,

        /// <summary>
        /// Indicates a general error occurred.
        /// </summary>
        Faulted = 2,

        /// <summary>
        /// Indicates that an unknown native error occurred.
        /// </summary>
        NativeError = 3,

        /// <summary>
        /// Indicates that the incoming request was not a valid WebSocket request.
        /// </summary>
        NotAWebSocket = 4,

        /// <summary>
        /// Indicates that the client requested an unsupported version of the WebSocket protocol.
        /// </summary>
        UnsupportedVersion = 5,

        /// <summary>
        /// Indicates that the client requested an unsupported WebSocket subprotocol.
        /// </summary>
        UnsupportedProtocol = 6,

        /// <summary>
        /// Indicates an error occurred while parsing the HTTP headers during the opening handshake.
        /// </summary>
        HeaderError = 7,

        /// <summary>
        /// Indicates that the connection was terminated unexpectedly.
        /// </summary>
        ConnectionClosedPrematurely = 8,

        /// <summary>
        /// Indicates the WebSocket is in an invalid state for the given operation (such as being closed or aborted).
        /// </summary>
        InvalidState = 9,
        #endregion
    }
}
