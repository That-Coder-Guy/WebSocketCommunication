using Microsoft.AspNetCore.Http;
using System.Net;
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
        /// Adds a web socket connection to be mananged as an asynchronous operation.
        /// </summary>
        /// <param name="context">The context handle to the web socket update request to be realized.</param>
        /// <returns>The relized web socket connection wrapped in a task.</returns>
        internal async Task<ServerWebSocket> Add(HttpContext context)
        {
            // Create the web socket connection
            ServerWebSocket webSocket = new ServerWebSocket(context);

            // Obtain the web socket collection access lock
            await _webSocketCollectionLock.WaitAsync();

            // Ensure the web socket is removed from the manager apon closure
            webSocket.Disconnected += async (s, e) => await Remove(webSocket);

            // Add the web socket connection to the manager
            _webSockets.Add(webSocket);

            // Release the web socket collection access lock
            _webSocketCollectionLock.Release();

            // Return the realized web socket connection
            return webSocket;
        }

        /// <summary>
        /// Removes a specific web socket connection for the manager as an asynchronous operation.
        /// </summary>
        /// <param name="item">The web socket connection to be removed.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private async Task Remove(ServerWebSocket item)
        {
            // Obtain the web socket collection access lock
            await _webSocketCollectionLock.WaitAsync();

            // Remove the target web socket
            _webSockets.Remove(item);

            // Release the web socket collection access lock
            _webSocketCollectionLock.Release();
        }

        /// <summary>
        /// Sends a message to all web socket connections in the manager as an asynchronous operation.
        /// </summary>
        /// <param name="message">The bytes making up the massage.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private async Task BroadcastAsync(byte[] message)
        {
            // Create a collection to contain all the send tasks
            List<Task> sendTasks = new List<Task>();

            // Obtain the web socket collection access lock
            await _webSocketCollectionLock.WaitAsync();
            try
            {
                foreach (ServerWebSocket socket in _webSockets)
                {
                    sendTasks.Add(socket.SendAsync(message));
                }
            }
            finally
            {
                // Release the web socket collection access lock
                _webSocketCollectionLock.Release();
            }

            // Wait for all send operations to finish
            await Task.WhenAll(sendTasks);
        }

        /// <summary>
        /// Sends a message to all web socket connections in the manager.
        /// </summary>
        /// <param name="message">The bytes making up the massage.</param>
        public void Broadcast(byte[] message)
        {
            Task.Run(() => BroadcastAsync(message));
        }

        /// <summary>
        /// Sends a message to the web socket connection with a specific identifier as an asynchronous operation.
        /// </summary>
        /// <param name="id">The identifier of the target web socket connection.</param>
        /// <param name="message">The bytes making up the massage.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private async Task SendToAsync(string id, byte[] message)
        {
            // Obtain the web socket collection access lock
            await _webSocketCollectionLock.WaitAsync();

            try
            {
                // Find the web socket connection with the specified identifier
                List<ServerWebSocket> matches = _webSockets.FindAll(socket => socket.Id == id);
                if (matches.Count == 1)
                {
                    await matches.First().SendAsync(message);
                }
            }
            finally
            {
                // Release the web socket collection access lock
                _webSocketCollectionLock.Release();
            }
        }

        /// <summary>
        /// Sends a message to the web socket connection with a specific identifier.
        /// </summary>
        /// <param name="id">The identifier of the target web socket connection.</param>
        /// <param name="message">The bytes making up the massage.</param>
        public void SendTo(string id, byte[] message)
        {
            Task.Run(() => SendToAsync(id, message));
        }

        /// <summary>
        /// Disconnects all the web socket connections in the manager as an asynchronous operation.
        /// </summary>
        private async Task DisconnectAllAsync()
        {
            List<Task> sendTasks = new List<Task>();

            // Obtain the web socket collection access lock
            await _webSocketCollectionLock.WaitAsync();
            try
            {
                foreach (ServerWebSocket socket in _webSockets)
                {
                    sendTasks.Add(socket.DisconnectAsync());
                }
            }
            finally
            {
                // Release the web socket collection access lock
                _webSocketCollectionLock.Release();
            }

            // Wait for all disconnect operations to finish
            await Task.WhenAll(sendTasks);
        }

        /// <summary>
        /// Disconnects all the web socket connections in the manager.
        /// </summary>
        private void DisconnectAll()
        {
            DisconnectAllAsync().GetAwaiter().GetResult();
        }
        #endregion
    }
}
