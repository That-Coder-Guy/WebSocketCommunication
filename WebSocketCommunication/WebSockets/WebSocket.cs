using System.Diagnostics;
using System.Net.WebSockets;
using WebSocketCommunication.Enumerations;
using WebSocketCommunication.EventArguments;
using SystemWebSocket = System.Net.WebSockets.WebSocket;
using SystemWebSocketState = System.Net.WebSockets.WebSocketState;
using WebSocketError = WebSocketCommunication.Enumerations.WebSocketError;
using WebSocketState = WebSocketCommunication.Enumerations.WebSocketState;

namespace WebSocketCommunication.WebSockets
{
    public abstract class WebSocket<TSource> where TSource : SystemWebSocket
    {
        #region Fields
        /// <summary>
        /// The buffer size for reading  and writting messages.
        /// </summary>
        private readonly int MESSAGE_BUFFER_SIZE = 16384;

        /// <summary>
        /// A handle for the asynchronous connection task.
        /// </summary>
        protected Task? _connectionTask;

        /// <summary>
        /// A handle for the asynchronous message listener task.
        /// </summary>
        private Task? _messageListenerTask;

        /// <summary>
        /// A cancellation token source for canceling the asynchronous connection task.
        /// </summary>
        protected CancellationTokenSource _connectionToken = new();

        /// <summary>
        /// A semaphore to prevent concurrent sending operations.
        /// </summary>
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        #endregion

        #region Events
        /// <summary>
        /// An event raised when the web socket establishes a connection with a web socket server.
        /// </summary>
        public virtual event EventHandler? Connected;

        /// <summary>
        /// An event raised when a message is received from a web socket server.
        /// </summary>
        public virtual event EventHandler<MessageEventArgs>? MessageReceived;

        /// <summary>
        /// An event raised when the web socket ends a connection with a web socket server.
        /// </summary>
        public virtual event EventHandler<DisconnectEventArgs>? Disconnected;

        /// <summary>
        /// An event raised when the web socket ends a connection with a web socket server.
        /// </summary>
        public virtual event EventHandler<ConnectionFailedEventArgs>? ConnectionFailed;
        #endregion

        #region Properties
        /// <summary>
        /// The internal web socket implementation being wrapped.
        /// </summary>
        protected abstract TSource InnerWebSocket { get; set; }

        /// <summary>
        /// The state of the wrapped web socket.
        /// </summary>
        public virtual WebSocketState State => (WebSocketState)InnerWebSocket.State;
        #endregion

        #region Methods
        /// <summary>
        /// Maps a WebSocketError to a corresponding WebSocketClosureReason.
        /// </summary>
        /// <param name="error">The error to convert.</param>
        /// <returns>The mapped closure reason.</returns>
        protected static WebSocketClosureReason GetClosureReason(WebSocketError error)
        {
            switch (error)
            {
                case WebSocketError.Success:
                    return WebSocketClosureReason.NormalClosure;

                case WebSocketError.InvalidMessageType:
                    return WebSocketClosureReason.InvalidMessageType;

                case WebSocketError.Faulted:
                case WebSocketError.NativeError:
                case WebSocketError.ConnectionClosedPrematurely:
                case WebSocketError.InvalidState:
                    return WebSocketClosureReason.InternalServerError;

                case WebSocketError.NotAWebSocket:
                case WebSocketError.UnsupportedVersion:
                case WebSocketError.UnsupportedProtocol:
                case WebSocketError.HeaderError:
                    return WebSocketClosureReason.ProtocolError;

                default:
                    return WebSocketClosureReason.InternalServerError;
            }
        }

        /// <summary>
        /// Checks the status of a nullable asynchrounous task.
        /// </summary>
        /// <param name="task">The task to check.</param>
        /// <returns>Whether the task is running or not.</returns>
        protected virtual bool IsTaskRunning(Task? task) => task != null && task.Status == TaskStatus.Running;

        /// <summary>
        /// Waits for a possibly null task to finish as an asynchrounous task.
        /// </summary>
        /// <param name="task">The nullable task.</param>
        /// <returns>Whether the task is running or not.</returns>
        protected virtual async Task WaitForTaskAsync(Task? task)
        {
            if (task != null && task.Status == TaskStatus.Running)
            {
                await task;
            }
        }

        /// <summary>
        /// Attempts to close the web socket connection in response to a WebSocketException as an asynchrounous task.
        /// </summary>
        /// <param name="exc">The WebSocketException that is the reason for he closure.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected virtual async Task EmergencyDisconnectAsync(WebSocketException exc)
        {
            if (InnerWebSocket.State != SystemWebSocketState.Closed)
            {
                await InnerWebSocket.CloseOutputAsync(WebSocketCloseStatus.InternalServerError, exc.Message, CancellationToken.None);
            }
            Disconnected?.Invoke(this, new DisconnectEventArgs(GetClosureReason((WebSocketError)exc.WebSocketErrorCode)));
        }

    /// <summary>
    /// Calls all methods attached to the Connected event.
    /// </summary>
    protected virtual void RaiseConnectedEvent() => Connected?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Calls all methods attached to the MessageReceived event.
        /// </summary>
        protected virtual void RaiseMessageRecievedEvent(MessageEventArgs args) => MessageReceived?.Invoke(this, args);

        /// <summary>
        /// Calls all methods attached to the Disconnected event.
        /// </summary>
        protected virtual void RaiseDisconnectedEvent(DisconnectEventArgs args) => Disconnected?.Invoke(this, args);

        /// <summary>
        /// Calls all methods attached to the ConnectionFailed event.
        /// </summary>
        protected virtual void RaiseConnectionFailedEvent(ConnectionFailedEventArgs args) => ConnectionFailed?.Invoke(this, args);

        /// <summary>
        /// Sends a message through the web socket connection as an asynchronous operation.
        /// </summary>
        /// <param name="message">The bytes of the message to be sent.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        internal virtual async Task SendAsync(byte[] message)
        {
            // Obtain the message sending lock
            await _sendLock.WaitAsync();

            try
            {
                // Calculate the total number of message chunks
                int totalChunks = (int)Math.Ceiling((double)message.Length / MESSAGE_BUFFER_SIZE);

                for (int i = 0; i < totalChunks; i++)
                {
                    // Determine the start and end positions of the chunk
                    int offset = i * MESSAGE_BUFFER_SIZE;
                    int length = Math.Min(MESSAGE_BUFFER_SIZE, message.Length - offset);

                    // Create a segment for the current chunk
                    ArraySegment<byte> bufferSegment = new ArraySegment<byte>(message, offset, length);

                    // Check if it's the final chunk
                    bool endOfMessage = (i == totalChunks - 1);

                    // Send the chunk asynchronously
                    await InnerWebSocket.SendAsync(bufferSegment, WebSocketMessageType.Binary, endOfMessage, CancellationToken.None);
                }
            }
            catch (WebSocketException exc)
            {
                Debug.Print(exc.Message);
                // Try to close WebSocket if it's not already closed
                await EmergencyDisconnectAsync(exc);
            }

            // Release the message sending lock
            _sendLock.Release();
        }

        /// <summary>
        /// Sends through the web socket connection.
        /// </summary>
        /// <param name="message"></param>
        public virtual void Send(byte[] message)
        {
            Task.Run(() => SendAsync(message));
        }

        /// <summary>
        /// Starts a message listener thread if it is not already running as an asynchronous operation.
        /// </summary>
        /// <returns>Whether the message listening process started.</returns>
        protected virtual bool BeginListening()
        {
            // If the listening task is not running
            if (!IsTaskRunning(_messageListenerTask))
            {
                // Start the listening task
                _messageListenerTask = Task.Run(ListenAsync);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Listens for messages while the web socket is open as an asynchronous operation.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected virtual async Task ListenAsync()
        {
            try
            {
                // Create a input buffer
                byte[] buffer = new byte[MESSAGE_BUFFER_SIZE];

                // Listen while the connection is open
                while (InnerWebSocket.State == SystemWebSocketState.Open)
                {
                    // Create a memory stream to store the incoming data
                    using (MemoryStream data = new MemoryStream())
                    {
                        // Wait to receive the initial message type and data to determin next steps
                        WebSocketReceiveResult result = await InnerWebSocket.ReceiveAsync(buffer, CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // Complete the closure handshake
                            switch (InnerWebSocket.State)
                            {
                                case SystemWebSocketState.Closed:
                                    // Disconnection handshake acknowledged
                                    break;

                                case SystemWebSocketState.CloseReceived:
                                    // Acknowledging disconnection handshake
                                    await InnerWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                                    break;
                            }
                            
                            // Raise the disconnected event
                            Disconnected?.Invoke(this, new DisconnectEventArgs(WebSocketClosureReason.NormalClosure));
                        }
                        else
                        {
                            // Read the rest of the message
                            data.Write(buffer, 0, result.Count);
                            while (!result.EndOfMessage)
                            {
                                result = await InnerWebSocket.ReceiveAsync(buffer, CancellationToken.None);
                                data.Write(buffer, 0, result.Count);
                            }

                            // Raise the message received event
                            MessageReceived?.Invoke(this, new MessageEventArgs(data));
                        }
                    }
                }
            }
            catch (WebSocketException exc)
            {
                Debug.Print($"On Listen : {exc.Message}");
                // Try to close WebSocket if it's not already closed
                await EmergencyDisconnectAsync(exc);
            }
        }

        /// <summary>
        /// Ends the web socket connection wht the web socket server as an asynchronous operation.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        internal virtual async Task DisconnectAsync()
        {
            // Obtain the message sending lock
            await _sendLock.WaitAsync();

            // Initiate the close handshake
            await InnerWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            
            // Wait for the message listening task to receive the closure acknowledgement and end
            await WaitForTaskAsync(_messageListenerTask);

            // Release the message sending lock
            _sendLock.Release();
        }

        /// <summary>
        /// Ends the web socket connection.
        /// </summary>
        public virtual void Disconnect()
        {
            DisconnectAsync().Wait();
        }
        #endregion
    }
}
