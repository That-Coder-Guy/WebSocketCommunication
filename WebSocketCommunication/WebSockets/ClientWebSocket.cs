using WebSocketException = System.Net.WebSockets.WebSocketException;
using SystemClientWebSocket = System.Net.WebSockets.ClientWebSocket;
using SystemWebSocketState = System.Net.WebSockets.WebSocketState;
using System.Diagnostics;

namespace WebSocketCommunication
{
    /// <summary>
    /// Represents a client-side WebSocket connection to a server.
    /// </summary>
    public class ClientWebSocket : WebSocket<SystemClientWebSocket>
    {
        #region Fields
        /// <summary>
        /// The URI of the server to which the client will connect.
        /// </summary>
        private Uri _serverUrl;

        /// <summary>
        /// A semaphore that ensures only one connection attempt is in progress at any given time.
        /// </summary>
        private SemaphoreSlim _connectionLock = new(1, 1);
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the underlying .NET ClientWebSocket instance.
        /// </summary>
        protected override SystemClientWebSocket InnerWebSocket { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientWebSocket"/> class.
        /// </summary>
        /// <param name="url">The URL of the server to connect to.</param>
        public ClientWebSocket(string url)
        {
            _serverUrl = new Uri(url);
            InnerWebSocket = new SystemClientWebSocket();
        }

        /// <summary>
        /// Asynchronously initiates a connection to the WebSocket server.
        /// </summary>
        /// <param name="token">A cancellation token to monitor while waiting for the connection to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous connection operation. The task result is a <see cref="WebSocketError"/>
        /// indicating the outcome of the connection attempt.
        /// </returns>
        private async Task<WebSocketError> ConnectAsync(CancellationToken token)
        {
            try
            {
                // Try to establish the connection to the WebSocket server using the provided cancellation token.
                await InnerWebSocket.ConnectAsync(_serverUrl, token);

                // Mark the WebSocket as connected.
                _isConnected = true;
            }
            catch (OperationCanceledException)
            {
                // If the connection attempt is canceled (e.g., due to a timeout),
                // reset the InnerWebSocket instance and return a Timeout error.
                InnerWebSocket = new SystemClientWebSocket();
                return WebSocketError.Timeout;
            }
            catch (WebSocketException webSocketExc)
            {
                // If a WebSocket-specific exception occurs during the connection attempt,
                // reset the InnerWebSocket instance and return the error code associated with the exception.
                InnerWebSocket = new SystemClientWebSocket();
                return (WebSocketError)webSocketExc.ErrorCode;
            }
            catch (Exception exc)
            {
                // For any other exception that occurs during the connection attempt,
                // reset the InnerWebSocket instance, log the exception details,
                // and return a general NativeError code.
                InnerWebSocket = new SystemClientWebSocket();
                Debug.Print($"Exception occurred during connection attempt: {exc.Message}");
                return WebSocketError.NativeError;
            }

            // If no exceptions occurred, the connection was successful.
            return WebSocketError.Success;
        }

        /// <summary>
        /// Attempts to establish a WebSocket connection within a specified timeout period.
        /// </summary>
        /// <param name="timeout">The maximum time (in milliseconds) allowed for the connection attempt.</param>
        /// <returns>A task representing the asynchronous connection attempt.</returns>
        public async Task AttemptConnect(int timeout)
        {
            // Ensure that we are not already connected.
            if (_isConnected)
            {
                throw new InvalidOperationException("Already connected.");
            }

            // Try to acquire the connection lock immediately to prevent concurrent connection attempts.
            if (!_connectionLock.Wait(0))
            {
                throw new InvalidOperationException("Connection attempt already in progress.");
            }

            // Create a cancellation token source for the connection attempt.
            CancellationTokenSource connectionTokenSource = new CancellationTokenSource();
            // Begin the asynchronous connection attempt.
            Task<WebSocketError> connectionTask = ConnectAsync(connectionTokenSource.Token);

            // Create a separate cancellation token source for the timeout delay.
            CancellationTokenSource timeoutTokenSource = new CancellationTokenSource();
            // Begin the timeout task.
            Task timeoutTask = Task.Delay(timeout, timeoutTokenSource.Token);

            // Wait for either the connection attempt or the timeout task to complete.
            Task completedTask = await Task.WhenAny(timeoutTask, connectionTask);

            // If the timeout task completed first, cancel the connection attempt.
            // Otherwise, cancel the timeout delay task.
            if (completedTask == timeoutTask)
            {
                connectionTokenSource.Cancel();
            }
            else
            {
                timeoutTokenSource.Cancel();
            }

            // Release the connection lock regardless of which task completed first.
            _connectionLock.Release();

            // Retrieve the connection result.
            WebSocketError error = connectionTask.Result;

            // Handle the connection result.
            switch (error)
            {
                case WebSocketError.Success:
                    // On a successful connection, raise the connected event and begin listening for messages.
                    RaiseConnectedEvent();
                    await ListenAsync();
                    break;

                default:
                    // On failure, raise a connection failed event with details.
                    RaiseConnectionFailedEvent(new ConnectionFailedEventArgs(error));
                    break;
            }
        }


        /// <summary>
        /// Initiates a WebSocket connection attempt with the specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time (in milliseconds) to wait for a successful connection.</param>
        public void Connect(int timeout)
        {
            // Run the connection attempt asynchronously.
            Task.Run(async () => await AttemptConnect(timeout));
        }
        #endregion
    }
}
