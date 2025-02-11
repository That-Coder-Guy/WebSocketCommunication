using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using WebSocketCommunication.WebSockets;

namespace WebSocketCommunication.Server
{
    public class WebSocketManager
    {
        #region Fields
        private List<ServerWebSocket> _webSockets = new();

        private Mutex _mutex = new Mutex();
        #endregion

        #region Properties
        public int Count => _webSockets.Count;
        #endregion

        #region Methods
        internal ServerWebSocket Add(HttpListenerContext context)
        {
            ServerWebSocket webSocket = new ServerWebSocket(context);
            webSocket.Disconnected += (s, e) => Remove(webSocket);
            _mutex.WaitOne();
            _webSockets.Add(webSocket);
            _mutex.ReleaseMutex();
            return webSocket;
        }

        private bool Remove(ServerWebSocket item)
        {
            _mutex.WaitOne();
            bool result = _webSockets.Remove(item);
            _mutex.ReleaseMutex();
            return result;
        }

        public void Broadcast(byte[] message)
        {
            _mutex.WaitOne();
            _webSockets.ForEach(socket => socket.Send(message));
            _mutex.ReleaseMutex();
        }
        #endregion
    }
}
