using System.Diagnostics;

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
        /// Gets the unique identifier for this WebSocket connection.
        /// </summary>
        protected string Id => _webSocket?.Id ?? string.Empty;

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
        /// Sends a binary message to the connected WebSocket client.
        /// </summary>
        /// <param name="message">The message to send, provided as a <see cref="MemoryStream"/>.</param>
        protected void Send(MemoryStream message)
        {
            _webSocket?.Send(message);
        }

        /// <summary>
        /// Sends a binary message to the connected WebSocket client.
        /// </summary>
        /// <param name="message">The message to send, provided as a byte array.</param>
        protected void Send(byte[] message)
        {
            _webSocket?.Send(message);
        }

        /// <summary>
        /// Invoked when the WebSocket connection is successfully established.
        /// Override this method to implement custom connection logic.
        /// </summary>
        /// <param name="sender">The source that raised the connection event.</param>
        /// <param name="e">Event data containing details about the connection event.</param>
        protected virtual void OnConnected(object? sender, EventArgs e)
        {
            Debug.Print($"Client [{Id}] connected.");
        }

        /// <summary>
        /// Invoked when the WebSocket connection attempt fails.
        /// Override this method to handle connection failures.
        /// </summary>
        /// <param name="sender">The source that raised the connection failure event.</param>
        /// <param name="e">Event data with details about the failure.</param>
        protected virtual void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
        {
            Debug.Print($"A client attempted to connect but failed.");
        }

        /// <summary>
        /// Invoked when a message is received over the WebSocket connection.
        /// Override this method to process incoming messages.
        /// </summary>
        /// <param name="sender">The source that raised the message event.</param>
        /// <param name="e">Event data containing the message details.</param>
        protected virtual void OnMessageReceived(object? sender, MessageEventArgs e)
        {
            Debug.Print($"A {e.Data.Length} byte message was received from cleint [{Id}].");
        }

        /// <summary>
        /// Invoked when the WebSocket connection is terminated or disconnected.
        /// Override this method to perform cleanup or notify clients.
        /// </summary>
        /// <param name="sender">The source that raised the disconnection event.</param>
        /// <param name="e">Event data containing details about the disconnection.</param>
        protected virtual void OnDisconnected(object? sender, DisconnectEventArgs e)
        {
            Debug.Print($"Client [{Id}] disconnected.");
        }
    }
}
