using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.InteropServices.Marshalling;
using WebSocketCommunication.Enumerations;
using WebSocketCommunication.EventArguments;
using WebSocketCommunication.Utilities;
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
        protected readonly int MESSAGE_BUFFER_SIZE = 16384; // Prpbably not the best place to put this variable

        /// <summary>
        /// A handle for the asynchronous message listener task.
        /// </summary>
        protected Task? _messageListenerTask;

        /// <summary>
        /// A cancellation token source for canceling the asynchronous message listener task.
        /// </summary>
        protected CancellationTokenSource _messageListenerToken = new();

        /// <summary>
        /// A handle for the asynchronous connection task.
        /// </summary>
        protected Task? _connectionTask;

        /// <summary>
        /// A cancellation token source for canceling the asynchronous connection task.
        /// </summary>
        protected CancellationTokenSource _connectionToken = new();
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
        protected static WebSocketClosureReason MapErrorToClosureReason(WebSocketError error)
        {
            return error switch
            {
                WebSocketError.Success => WebSocketClosureReason.NormalClosure,
                WebSocketError.InvalidMessageType => WebSocketClosureReason.InvalidMessageType,
                WebSocketError.Faulted => WebSocketClosureReason.InternalServerError,
                WebSocketError.NativeError => WebSocketClosureReason.InternalServerError,
                WebSocketError.NotAWebSocket => WebSocketClosureReason.ProtocolError,
                WebSocketError.UnsupportedVersion => WebSocketClosureReason.PolicyViolation,
                WebSocketError.UnsupportedProtocol => WebSocketClosureReason.PolicyViolation,
                WebSocketError.HeaderError => WebSocketClosureReason.ProtocolError,
                WebSocketError.ConnectionClosedPrematurely => WebSocketClosureReason.EndpointUnavailable,
                WebSocketError.InvalidState => WebSocketClosureReason.Empty,
                _ => WebSocketClosureReason.Empty // Default case
            };
        }

        protected virtual bool IsTaskRunning(Task? task) => task == null || task.Status != TaskStatus.Running;

        protected virtual bool CancelTask(Task? task, CancellationTokenSource source)
        {
            if (task != null && !task.IsCompleted)
            {
                source.Cancel();
                task.Wait();
                return task.IsCanceled;
            }
            return false;
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
        /// Sends a message to the web socket server as an asynchronous operation.
        /// </summary>
        /// <param name="message">The bytes of the message to be sent.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected virtual async Task SendAsync(byte[] message)
        {
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

        /// <summary>
        /// Sends a message to the web socket server.
        /// </summary>
        /// <param name="message">The bytes of the message to be sent.</param>
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
                // Create a cancellation token for the listening task
                _messageListenerToken = new CancellationTokenSource();

                // Start the listening task
                _messageListenerTask = Task.Run(() => ListenAsync(_messageListenerToken.Token));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Listens for messages while the web socket is open as an asynchronous operation.
        /// </summary>
        /// <param name="token">A cancellation token used to propagate notification that the operation should be canceled.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected virtual async Task ListenAsync(CancellationToken token)
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
                        WebSocketReceiveResult result = await InnerWebSocket.ReceiveAsync(buffer, token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // Complete the closure handshake
                            await InnerWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);

                            // Raise the disconnected event
                            Disconnected?.Invoke(this, new DisconnectEventArgs(WebSocketClosureReason.NormalClosure));
                        }
                        else
                        {
                            // Read the rest of the message
                            data.Write(buffer, 0, result.Count);
                            while (!result.EndOfMessage)
                            {
                                result = await InnerWebSocket.ReceiveAsync(buffer, token);
                                data.Write(buffer, 0, result.Count);
                            }

                            // Raise the message received event
                            MessageReceived?.Invoke(this, new MessageEventArgs(data));
                        }
                    }
                }
            }
            catch (WebSocketException ex)
            {
                // If the web socket throws an error
                Debug.Print($"WebSocket error: {ex.Message}");

                // Try to close WebSocket if it's not already closed
                if (InnerWebSocket.State != SystemWebSocketState.CloseReceived)
                {
                    await InnerWebSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Error during communication", CancellationToken.None);
                }
            }
            catch (OperationCanceledException)
            {
                // If the listening task is canceled
                Debug.Print("Listening process canceled.");
            }
        }

        /// <summary>
        /// Stops the message listener thread if it is running.
        /// </summary>
        /// <returns>Whether the message listening process ended.</returns>
        protected virtual bool EndListening()
        {
            return CancelTask(_messageListenerTask, _messageListenerToken);
        }

        /// <summary>
        /// Ends the web socket connection wht the web socket server as an asynchronous operation.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual async Task DisconnectAsync()
        {
            EndListening();
            await InnerWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }

        /// <summary>
        /// Ends the web socket connection wht the web socket server.
        /// </summary>
        public virtual void Disconnect()
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }
        #endregion
    }
}
