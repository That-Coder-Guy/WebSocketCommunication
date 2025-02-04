using System.Collections;
using System.Collections.Concurrent;

namespace WebSocketCommunication
{
    public class WebSocketCollection
    {
        #region Fields
        private List<WebSocket> _webSockets = new();

        private Semaphore _semaphore
        #endregion

        #region Properties
        public int Count => _webSockets.Count;
        #endregion

        #region Methods
        public void Add(WebSocket item)
        {

            _webSockets.Add(item);
        }

        public bool Remove(WebSocket item)
        {
            _webSockets.Remove(item);
        }
        #endregion


    }
}
