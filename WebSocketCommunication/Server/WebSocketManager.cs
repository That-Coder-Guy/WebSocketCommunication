using Microsoft.AspNetCore.Http;

namespace WebSocketCommunication.Server
{
    /// <summary>
    /// Manages active WebSocket connections for server implementations.
    /// </summary>
    public class WebSocketManager
    {
        #region Fields
        /// <summary>
        /// Holds all active WebSocket connections.
        /// </summary>
        private List<ServerWebSocket> _webSockets = new();

        /// <summary>
        /// Ensures thread-safe access to the WebSocket collection.
        /// </summary>
        private SemaphoreSlim _webSocketCollectionLock = new SemaphoreSlim(1, 1);
        #endregion

        #region Properties
        /// <summary>
        /// Gets the current count of managed WebSocket connections.
        /// </summary>
        public int Count => _webSockets.Count;
        #endregion

        #region Methods
        /// <summary>
        /// Asynchronously creates and registers a new WebSocket connection based on the incoming HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context for the WebSocket upgrade request.</param>
        /// <returns>A task that represents the asynchronous operation, returning the created WebSocket connection.</returns>
        internal async Task<ServerWebSocket> Add(HttpContext context)
        {
            // Instantiate a new WebSocket connection.
            ServerWebSocket webSocket = new ServerWebSocket(context);

            // Acquire the lock to ensure exclusive access to the collection.
            await _webSocketCollectionLock.WaitAsync();

            // Ensure that the connection is removed from the manager when it disconnects.
            webSocket.Disconnected += async (s, e) => await Remove(webSocket);

            // Add the new WebSocket connection to the managed collection.
            _webSockets.Add(webSocket);

            // Release the lock on the collection.
            _webSocketCollectionLock.Release();

            // Return the newly created WebSocket connection.
            return webSocket;
        }

        /// <summary>
        /// Asynchronously removes the specified WebSocket connection from the manager.
        /// </summary>
        /// <param name="item">The WebSocket connection to remove.</param>
        /// <returns>A task representing the asynchronous removal operation.</returns>
        private async Task Remove(ServerWebSocket item)
        {
            // Acquire the lock to modify the collection safely.
            await _webSocketCollectionLock.WaitAsync();

            // Remove the specified WebSocket connection.
            _webSockets.Remove(item);

            // Release the lock.
            _webSocketCollectionLock.Release();
        }

        /// <summary>
        /// Asynchronously broadcasts a message to all active WebSocket connections using a MemoryStream.
        /// </summary>
        /// <param name="message">A MemoryStream containing the message bytes to send.</param>
        /// <returns>A task representing the asynchronous broadcast operation.</returns>
        private async Task BroadcastAsync(MemoryStream message)
        {
            // Collect tasks for sending the message to each connection.
            List<Task> sendTasks = new List<Task>();

            // Lock the collection during iteration to prevent modifications.
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
                // Always release the lock, even if an exception occurs.
                _webSocketCollectionLock.Release();
            }

            // Wait until all send operations have completed.
            await Task.WhenAll(sendTasks);
        }

        /// <summary>
        /// Broadcasts a message to all active WebSocket connections asynchronously using a <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="message">A MemoryStream containing the message bytes to send.</param>
        public void Broadcast(MemoryStream message)
        {
            // Run the asynchronous broadcast operation without awaiting it.
            Task.Run(() => BroadcastAsync(message));
        }

        /// <summary>
        /// Broadcasts a message to all active WebSocket connections asynchronously using a byte array.
        /// </summary>
        /// <param name="message">The message bytes to send.</param>
        public void Broadcast(byte[] message)
        {
            // Run the asynchronous broadcast operation without awaiting it.
            Task.Run(() => BroadcastAsync(new MemoryStream(message)));
        }

        /// <summary>
        /// Asynchronously sends a message to a specific WebSocket connection identified by its ID.
        /// </summary>
        /// <param name="id">The identifier of the target WebSocket connection.</param>
        /// <param name="message">The message bytes to send.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        private async Task SendToAsync(string id, MemoryStream message)
        {
            // Acquire the lock to search the connection collection safely.
            await _webSocketCollectionLock.WaitAsync();

            try
            {
                // Locate the connection(s) matching the provided ID.
                List<ServerWebSocket> matches = _webSockets.FindAll(socket => socket.Id == id);
                if (matches.Count == 1)
                {
                    // Send the message to the matching connection.
                    await matches.First().SendAsync(message);
                }
            }
            finally
            {
                // Release the lock regardless of the operation outcome.
                _webSocketCollectionLock.Release();
            }
        }

        /// <summary>
        /// Sends a message to a specific WebSocket connection identified by its ID.
        /// </summary>
        /// <param name="id">The identifier of the target WebSocket connection.</param>
        /// <param name="message">The message bytes to send.</param>
        public void SendTo(string id, byte[] message)
        {
            // Initiate the asynchronous send operation without awaiting it.
            Task.Run(() => SendToAsync(id, new MemoryStream(message)));
        }

        /// <summary>
        /// Asynchronously disconnects all active WebSocket connections.
        /// </summary>
        /// <returns>A task representing the asynchronous disconnect operations.</returns>
        private async Task DisconnectAllAsync()
        {
            // Gather tasks for disconnecting each connection.
            List<Task> disconnectTasks = new List<Task>();

            // Acquire the lock to iterate over the connections safely.
            await _webSocketCollectionLock.WaitAsync();
            try
            {
                foreach (ServerWebSocket socket in _webSockets)
                {
                    disconnectTasks.Add(socket.DisconnectAsync());
                }
            }
            finally
            {
                // Release the lock after iterating.
                _webSocketCollectionLock.Release();
            }

            // Wait for all disconnect operations to complete.
            await Task.WhenAll(disconnectTasks);
        }

        /// <summary>
        /// Synchronously disconnects all active WebSocket connections.
        /// </summary>
        private void DisconnectAll()
        {
            // Wait synchronously for all asynchronous disconnect operations to finish.
            DisconnectAllAsync().GetAwaiter().GetResult();
        }
        #endregion
    }
}
