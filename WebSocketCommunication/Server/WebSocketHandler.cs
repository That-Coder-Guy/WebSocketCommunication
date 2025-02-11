using System.Net;
using WebSocketCommunication.EventArguments;
using WebSocketCommunication.WebSockets;

namespace WebSocketCommunication.Server
{
    public abstract class WebSocketHandler
    {
        private ServerWebSocket? _webSocket;

        protected WebSocketManager Clients { get; } = new WebSocketManager();

        internal void Start(HttpListenerContext context)
        {
            _webSocket = new ServerWebSocket(context);

            _webSocket.Connected += OnConnected;
            _webSocket.ConnectionFailed += OnConnectionFailed;
            _webSocket.MessageReceived += OnMessageReceived;
            _webSocket.Disconnected += OnDisconnected;
        }

        protected abstract void OnConnected(object? sender, EventArgs e);

        protected abstract void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e);

        protected abstract void OnMessageReceived(object? sender, MessageEventArgs e);

        protected abstract void OnDisconnected(object? sender, DisconnectEventArgs e);
    }
}
