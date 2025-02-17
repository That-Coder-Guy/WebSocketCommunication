namespace WebSocketCommunication.Server
{
    /// <summary>
    /// Serves as the abstract base for handling WebSocket connection events in server applications.
    /// </summary>
    public abstract class WebSocketHandler
    {
        /// <summary>
        /// The active WebSocket connection that this handler manages.
        /// </summary>
        private ServerWebSocket? _webSocket;

        /// <summary>
        /// The manager responsible for tracking all active WebSocket connections on the server.
        /// </summary>
        protected WebSocketManager Clients { get; private set; } = new();

        /// <summary>
        /// Associates a specific WebSocket connection and its connection manager with this handler.
        /// Registers the necessary event callbacks for handling connection lifecycle events.
        /// </summary>
        /// <param name="webSocket">The WebSocket connection to manage.</param>
        /// <param name="manager">The manager that tracks all server WebSocket connections.</param>
        internal void Attach(ServerWebSocket webSocket, WebSocketManager manager)
        {
            _webSocket = webSocket;
            Clients = manager;
            _webSocket.Connected += OnConnected;
            _webSocket.ConnectionFailed += OnConnectionFailed;
            _webSocket.MessageReceived += OnMessageReceived;
            _webSocket.Disconnected += OnDisconnected;
        }

        /// <summary>
        /// Invoked when the WebSocket connection is successfully established.
        /// Override this method to implement custom connection logic.
        /// </summary>
        /// <param name="sender">The source that raised the connection event.</param>
        /// <param name="e">Event data containing details about the connection event.</param>
        protected abstract void OnConnected(object? sender, EventArgs e);

        /// <summary>
        /// Invoked when the WebSocket connection attempt fails.
        /// Override this method to handle connection failures.
        /// </summary>
        /// <param name="sender">The source that raised the connection failure event.</param>
        /// <param name="e">Event data with details about the failure.</param>
        protected abstract void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e);

        /// <summary>
        /// Invoked when a message is received over the WebSocket connection.
        /// Override this method to process incoming messages.
        /// </summary>
        /// <param name="sender">The source that raised the message event.</param>
        /// <param name="e">Event data containing the message details.</param>
        protected abstract void OnMessageReceived(object? sender, MessageEventArgs e);

        /// <summary>
        /// Invoked when the WebSocket connection is terminated or disconnected.
        /// Override this method to perform cleanup or notify clients.
        /// </summary>
        /// <param name="sender">The source that raised the disconnection event.</param>
        /// <param name="e">Event data containing details about the disconnection.</param>
        protected abstract void OnDisconnected(object? sender, DisconnectEventArgs e);
    }
}
