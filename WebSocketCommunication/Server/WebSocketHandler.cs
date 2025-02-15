using System.Net;
using WebSocketCommunication.EventArguments;
using WebSocketCommunication.WebSockets;

namespace WebSocketCommunication.Server
{
    /// <summary>
    /// A comprehensive web socket event handler for server implementations.
    /// </summary>
    public abstract class WebSocketHandler
    {
        /// <summary>
        /// The web socket connection to handle.
        /// </summary>
        private ServerWebSocket? _webSocket;

        /// <summary>
        /// A collection of all the web socket connections managed by the server.
        /// </summary>
        protected WebSocketManager Clients { get; private set; } = new();


        /// <summary>
        /// Attaches the web socket connection to be handled.
        /// </summary>
        /// <param name="webSocket">The target web socket connection.</param>
        /// <param name="manager">A collection of all the web socket connections managed by the server.</param>
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
        /// A method that receives the connection event of the web socket connection being handled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void OnConnected(object? sender, EventArgs e);

        protected abstract void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e);

        protected abstract void OnMessageReceived(object? sender, MessageEventArgs e);

        protected abstract void OnDisconnected(object? sender, DisconnectEventArgs e);
    }
}
