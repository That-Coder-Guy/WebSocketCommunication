using System.Net;
using WebSocketCommunication.Utilities;
using Connection = System.Net.WebSockets.WebSocket;

namespace WebSocketCommunication.Communication
{
    public abstract class WebSocketHandler
    {
        private WebSocket? _webSocket;

        internal void Start(HttpListenerContext context)
        {
            _webSocket = new WebSocket(context);

            _webSocket.Connected += OnConnected;
            _webSocket.MessageReceived += OnMessageReceived;
            _webSocket.Disconnected += OnDisconnected;
        }

        public abstract void OnConnected();

        public abstract void OnMessageReceived(object? sender, MessageEventArgs e);

        public abstract void OnDisconnected();
    }
}
