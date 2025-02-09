using System.Net;
using WebSocketCommunication.Communication;
using WebSocketCommunication.Utilities;
using Connection = System.Net.WebSockets.WebSocket;

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
            _webSocket.MessageReceived += OnMessageReceived;
            _webSocket.Disconnected += OnDisconnected;
        }

        protected abstract void OnConnected(object? sender, EventArgs e);

        protected abstract void OnMessageReceived(object? sender, MessageEventArgs e);

        protected abstract void OnDisconnected(object? sender, EventArgs e);
    }
}
