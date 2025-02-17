namespace WebSocketCommunication
{
    /// <summary>
    /// Provides data for the event that is raised when a WebSocket message is received.
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the data contained in the received WebSocket message.
        /// </summary>
        public MemoryStream Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageEventArgs"/> class with the specified message data.
        /// </summary>
        /// <param name="data">The data received from the WebSocket message.</param>
        public MessageEventArgs(MemoryStream data)
        {
            Data = data;
        }
    }
}

