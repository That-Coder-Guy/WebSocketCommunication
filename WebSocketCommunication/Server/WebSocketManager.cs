using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using WebSocketCommunication.WebSockets;

namespace WebSocketCommunication.Server
{
    /// <summary>
    /// A web socket connection manager for server implementations.
    /// </summary>
    public class WebSocketManager
    {
        #region Fields
        /// <summary>
        /// A collection of all the web socket connection being managed.
        /// </summary>
        private List<ServerWebSocket> _webSockets = new();

        /// <summary>
        /// An access lock for the web socket connection collection.
        /// </summary>
        private SemaphoreSlim _webSocketCollectionLock = new SemaphoreSlim(1, 1);
        #endregion

        #region Properties
        /// <summary>
        /// The number of web socket connections being managed.
        /// </summary>
        public int Count => _webSockets.Count;
        #endregion

        #region Methods
        /// <summary>
        /// Adds a web socket connection to be mananged.
        /// </summary>
        /// <param name="context">The context handle to the web socket update request to be realized.</param>
        /// <returns>The relized web socket connections.</returns>
        internal async Task<ServerWebSocket> Add(HttpListenerContext context)
        {
            // Create the web socket connection
            ServerWebSocket webSocket = new ServerWebSocket(context);

            // Obtain the web socket collection access lock.
            await _webSocketCollectionLock.WaitAsync();

            // Ensure the web socket is removed from the manager apon closure
            webSocket.Disconnected += (s, e) => Task.Run(() => Remove(webSocket));

            // Add the web socket connection to the manager
            _webSockets.Add(webSocket);

            // Release the web socket collection access lock.
            _webSocketCollectionLock.Release();

            // Return the realized web socket connection.
            return webSocket;
        }

        private async Task Remove(ServerWebSocket item)
        {
            // Obtain the web socket collection access lock.
            await _webSocketCollectionLock.WaitAsync();

            // Remove the target web socket
            _webSockets.Remove(item);

            // Release the web socket collection access lock.
            _webSocketCollectionLock.Release();
        }

        public async Task BroadcastAsync(byte[] message)
        {
            // Obtain the web socket collection access lock.
            await _webSocketCollectionLock.WaitAsync();

            _webSockets.ForEach(socket => socket.Send(message));

            // Release the web socket collection access lock.
            _webSocketCollectionLock.Release();
        }

        public void Broadcast(byte[] message)
        {
            Task.Run(() => BroadcastAsync(message));
        }

        /// <summary>
        /// Disconnects all the web socket connections in the manager.
        /// </summary>
        public void DisconnectAll()
        {
            // Obtain the web socket collection access lock.
            _webSocketCollectionLock.Wait();

            _webSockets.ForEach(socket => socket.Disconnect());

            // Release the web socket collection access lock.
            _webSocketCollectionLock.Release();
        }
        #endregion
    }
}
