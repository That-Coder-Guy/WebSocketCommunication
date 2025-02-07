using Connection = System.Net.WebSockets.WebSocket;

namespace WebSocketCommunication.Communication
{
    public abstract class WebSocketHandler
    {
        private WebSocket _webSocket;

        internal WebSocketHandler(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }
    }
}
