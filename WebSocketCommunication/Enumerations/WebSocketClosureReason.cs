namespace WebSocketCommunication
{
    /// <summary>
    /// Specifies the possible reasons for a WebSocket connection closure.
    /// </summary>
    public enum WebSocketClosureReason
    {
        /// <summary>
        /// (1000) The connection has closed after the request was successfully fulfilled.
        /// </summary>
        NormalClosure = 1000,

        /// <summary>
        /// (1001) Indicates that an endpoint is being removed. Either the server or client will become unavailable.
        /// </summary>
        EndpointUnavailable = 1001,

        /// <summary>
        /// (1002) The client or server is terminating the connection due to a protocol error.
        /// </summary>
        ProtocolError = 1002,

        /// <summary>
        /// (1003) The client or server is terminating the connection because it cannot accept the received data type.
        /// </summary>
        InvalidMessageType = 1003,

        /// <summary>
        /// No specific error code provided.
        /// </summary>
        Empty = 1005,

        /// <summary>
        /// (1007) The client or server is terminating the connection due to receiving data that is inconsistent with the message type.
        /// </summary>
        InvalidPayloadData = 1007,

        /// <summary>
        /// (1008) The connection will be closed because an endpoint has received a message that violates its policy.
        /// </summary>
        PolicyViolation = 1008,

        /// <summary>
        /// (1009) The client or server is terminating the connection due to receiving a message that exceeds the allowable size.
        /// </summary>
        MessageTooBig = 1009,

        /// <summary>
        /// (1010) The client is terminating the connection because it expected the server to negotiate an extension that was not provided.
        /// </summary>
        MandatoryExtension = 1010,

        /// <summary>
        /// (1011) The server is terminating the connection due to an internal server error.
        /// </summary>
        InternalServerError = 1011
    }
}
