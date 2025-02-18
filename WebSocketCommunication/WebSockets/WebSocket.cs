using System.Net.WebSockets;
using SystemWebSocket = System.Net.WebSockets.WebSocket;
using SystemWebSocketState = System.Net.WebSockets.WebSocketState;

namespace WebSocketCommunication
{
    public abstract class WebSocket<TSource> where TSource : SystemWebSocket
    {
        #region Fields
        /// <summary>
        /// Defines the size of the buffer used for both reading and writing message data.
        /// </summary>
        private readonly int MESSAGE_BUFFER_SIZE = 16384;

        /// <summary>
        /// Holds the asynchronous task that manages the connection process.
        /// </summary>
        protected Task? _connectionTask;

        /// <summary>
        /// Holds the asynchronous task that listens for incoming messages.
        /// </summary>
        private Task? _messageListenerTask;

        /// <summary>
        /// Provides a token to cancel ongoing asynchronous connection operations.
        /// </summary>
        protected CancellationTokenSource _connectionToken = new();

        /// <summary>
        /// Ensures that only one send operation is active at any given time.
        /// </summary>
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the WebSocket successfully establishes a connection.
        /// </summary>
        public virtual event EventHandler? Connected;

        /// <summary>
        /// Occurs when a message is received from the WebSocket server.
        /// </summary>
        public virtual event EventHandler<MessageEventArgs>? MessageReceived;

        /// <summary>
        /// Occurs when the WebSocket connection is closed.
        /// </summary>
        public virtual event EventHandler<DisconnectEventArgs>? Disconnected;

        /// <summary>
        /// Occurs when the WebSocket fails to establish a connection.
        /// </summary>
        public virtual event EventHandler<ConnectionFailedEventArgs>? ConnectionFailed;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the underlying WebSocket instance that is being wrapped.
        /// </summary>
        protected abstract TSource InnerWebSocket { get; set; }

        /// <summary>
        /// Gets the current connection state of the wrapped WebSocket.
        /// </summary>
        public virtual WebSocketState State => (WebSocketState)InnerWebSocket.State;
        #endregion

        #region Methods
        /// <summary>
        /// Converts a given <see cref="WebSocketError"/> into its corresponding closure reason.
        /// </summary>
        /// <param name="error">The error code to map.</param>
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
        /// Determines whether the specified task is currently running.
        /// </summary>
        /// <param name="task">The task to evaluate.</param>
        /// <returns><c>true</c> if the task is running; otherwise, <c>false</c>.</returns>
        protected virtual bool IsTaskRunning(Task? task) => task != null && task.Status == TaskStatus.Running;

        /// <summary>
        /// Asynchronously waits for the provided task to complete if it is currently running.
        /// </summary>
        /// <param name="task">The task to wait for.</param>
        protected virtual async Task WaitForTaskAsync(Task? task)
        {
            if (task != null && task.Status == TaskStatus.Running)
            {
                await task;
            }
        }

        /// <summary>
        /// Handles a WebSocketException by attempting an immediate disconnection.
        /// Logs the exception message, performs the closing handshake if necessary, 
        /// and raises the Disconnected event with the appropriate closure reason.
        /// </summary>
        /// <param name="exc">The WebSocketException that triggered the disconnect.</param>
        protected virtual async Task EmergencyDisconnectAsync(WebSocketException exc)
        {
            if (InnerWebSocket.State != SystemWebSocketState.Closed)
            {
                // Attempt to close the output channel using an error status.
                await InnerWebSocket.CloseOutputAsync(WebSocketCloseStatus.InternalServerError, exc.Message, CancellationToken.None);
            }
            // Raise the disconnected event with the mapped closure reason.
            RaiseDisconnectedEvent(new DisconnectEventArgs(GetClosureReason((WebSocketError)exc.WebSocketErrorCode)));
        }

        /// <summary>
        /// Asynchronously invokes all subscribers of the Connected event.
        /// </summary>
        protected virtual void RaiseConnectedEvent() =>
            Task.Run(() => Connected?.Invoke(this, EventArgs.Empty));

        /// <summary>
        /// Asynchronously invokes all subscribers of the MessageReceived event with the provided arguments.
        /// </summary>
        /// <param name="args">The event arguments containing the received message data.</param>
        protected virtual void RaiseMessageReceivedEvent(MessageEventArgs args) =>
            Task.Run(() => MessageReceived?.Invoke(this, args));

        /// <summary>
        /// Asynchronously invokes all subscribers of the Disconnected event with the provided arguments.
        /// </summary>
        /// <param name="args">The event arguments containing disconnection details.</param>
        protected virtual void RaiseDisconnectedEvent(DisconnectEventArgs args) =>
            Task.Run(() => Disconnected?.Invoke(this, args));

        /// <summary>
        /// Asynchronously invokes all subscribers of the ConnectionFailed event with the provided arguments.
        /// </summary>
        /// <param name="args">The event arguments containing failure details.</param>
        protected virtual void RaiseConnectionFailedEvent(ConnectionFailedEventArgs args) =>
            Task.Run(() => ConnectionFailed?.Invoke(this, args));

        /// <summary>
        /// Asynchronously sends a binary message over the WebSocket connection.
        /// The message is segmented into chunks if its size exceeds the buffer limit.
        /// In case of an error, an emergency disconnect is initiated.
        /// </summary>
        /// <param name="message">The message data to send as a byte array.</param>
        internal virtual async Task SendAsync(byte[] message)
        {
            // Acquire the lock to ensure exclusive access for sending.
            await _sendLock.WaitAsync();

            try
            {
                // Determine the number of segments required based on the buffer size.
                int totalChunks = (int)Math.Ceiling((double)message.Length / MESSAGE_BUFFER_SIZE);

                for (int i = 0; i < totalChunks; i++)
                {
                    // Calculate the offset and length for the current chunk.
                    int offset = i * MESSAGE_BUFFER_SIZE;
                    int length = Math.Min(MESSAGE_BUFFER_SIZE, message.Length - offset);

                    // Create a segment representing the current chunk of data.
                    ArraySegment<byte> bufferSegment = new ArraySegment<byte>(message, offset, length);

                    // Identify if this is the final segment of the message.
                    bool endOfMessage = (i == totalChunks - 1);

                    // Transmit the current chunk asynchronously.
                    await InnerWebSocket.SendAsync(bufferSegment, WebSocketMessageType.Binary, endOfMessage, CancellationToken.None);
                }
            }
            catch (WebSocketException exc)
            {
                // If an error occurs, initiate an emergency disconnect.
                await EmergencyDisconnectAsync(exc);
            }

            // Release the send lock after the message has been sent.
            _sendLock.Release();
        }

        /// <summary>
        /// Initiates the asynchronous sending of a message over the WebSocket connection.
        /// </summary>
        /// <param name="message">The message data to send as a byte array.</param>
        public virtual void Send(byte[] message)
        {
            Task.Run(() => SendAsync(message));
        }

        /// <summary>
        /// Continuously listens for incoming messages while the WebSocket connection remains open.
        /// Handles message fragments, close frames, and triggers appropriate events based on the received data.
        /// </summary>
        protected virtual async Task ListenAsync()
        {
            try
            {
                // Allocate a buffer for receiving message data.
                byte[] buffer = new byte[MESSAGE_BUFFER_SIZE];

                // Continue listening while the connection remains open.
                while (InnerWebSocket.State == SystemWebSocketState.Open)
                {
                    // Use a memory stream to accumulate data from potentially fragmented messages.
                    using (MemoryStream data = new MemoryStream())
                    {
                        // Receive the first segment of the message.
                        WebSocketReceiveResult result = await InnerWebSocket.ReceiveAsync(buffer, CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // Handle the closing handshake based on the current state.
                            switch (InnerWebSocket.State)
                            {
                                case SystemWebSocketState.Closed:
                                    // The close handshake has already been acknowledged.
                                    break;

                                case SystemWebSocketState.CloseReceived:
                                    // Acknowledge the received close frame by sending a close message.
                                    await InnerWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                                    break;
                            }

                            // Notify subscribers that the connection has been closed.
                            RaiseDisconnectedEvent(new DisconnectEventArgs(WebSocketClosureReason.NormalClosure));
                        }
                        else
                        {
                            // Write the received data to the memory stream.
                            data.Write(buffer, 0, result.Count);

                            // Continue reading if the message is fragmented.
                            while (!result.EndOfMessage)
                            {
                                result = await InnerWebSocket.ReceiveAsync(buffer, CancellationToken.None);
                                data.Write(buffer, 0, result.Count);
                            }

                            // Notify subscribers that a complete message has been received.
                            RaiseMessageReceivedEvent(new MessageEventArgs(data));
                        }
                    }
                }
            }
            catch (WebSocketException exc)
            {
                // On error, perform an emergency disconnect.
                await EmergencyDisconnectAsync(exc);
            }
        }

        /// <summary>
        /// Gracefully disconnects the WebSocket connection as an asynchronous operation.
        /// Acquires a lock to prevent simultaneous send operations, initiates the closing handshake,
        /// and waits for the message listener to terminate.
        /// </summary>
        internal virtual async Task DisconnectAsync()
        {
            // Acquire the send lock to ensure no messages are being sent during disconnect.
            await _sendLock.WaitAsync();

            // Initiate the closing handshake with a normal closure status.
            await InnerWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);

            // Wait for the message listening task to complete, ensuring the close handshake is acknowledged.
            await WaitForTaskAsync(_messageListenerTask);

            // Release the send lock.
            _sendLock.Release();
        }

        /// <summary>
        /// Synchronously disconnects the WebSocket connection by waiting for the asynchronous disconnect to complete.
        /// </summary>
        public virtual void Disconnect()
        {
            DisconnectAsync().Wait();
        }
        #endregion
    }
}
