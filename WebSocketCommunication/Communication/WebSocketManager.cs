using System.Collections;
using System.Collections.Concurrent;

namespace WebSocketCommunication.Communication
{
    public class WebSocketManager
    {
        #region Fields
        private List<WebSocket> _webSockets = new();

        private Mutex _mutex = new Mutex();
        #endregion

        #region Properties
        public int Count => _webSockets.Count;
        #endregion

        #region Methods
        public void Add(WebSocket item)
        {
            _mutex.WaitOne();
            _webSockets.Add(item);
            _mutex.ReleaseMutex();
        }

        public bool Remove(WebSocket item)
        {
            _mutex.WaitOne();
            bool result = _webSockets.Remove(item);
            _mutex.ReleaseMutex();
            return result;
        }
        #endregion
    }
}
