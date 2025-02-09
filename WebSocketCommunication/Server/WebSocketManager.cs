using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using WebSocketCommunication.Communication;

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
        internal void Add(HttpListenerContext context)
        {
            _mutex.WaitOne();
            ServerWebSocket webSocket = new ServerWebSocket(context);
            webSocket.

            _webSockets.Add(item);
            _mutex.ReleaseMutex();
        }

        public bool Remove(ServerWebSocket item)
        {
            _mutex.WaitOne();
            bool result = _webSockets.Remove(item);
            _mutex.ReleaseMutex();
            return result;
        }
        #endregion
    }
}
