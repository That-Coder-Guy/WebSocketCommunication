using WebSocketException = System.Net.WebSockets.WebSocketException;
using SystemClientWebSocket = System.Net.WebSockets.ClientWebSocket;
using SystemWebSocketState = System.Net.WebSockets.WebSocketState;

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
        /// Asynchronously initiates the connection to the WebSocket server.
        /// </summary>
        /// <param name="token">A token to observe while waiting for the connection to complete.</param>
        /// <returns>A task that represents the asynchronous connection operation.</returns>
        private async Task ConnectAsync(CancellationToken token)
        {
            Logger.Log("Initiating connection process...");
            try
            {
                // Attempt to establish the connection.
                await InnerWebSocket.ConnectAsync(_serverUrl, token);

                // Check the state of the WebSocket after the connection attempt.
                switch (InnerWebSocket.State)
                {
                    case SystemWebSocketState.Open:
                        Logger.Log("Connection established successfully.");
                        RaiseConnectedEvent();
                        // Start listening for incoming messages.
                        await ListenAsync();
                        break;

                    default:
                        Logger.Log("Connection attempt failed.");
                        RaiseConnectionFailedEvent(new ConnectionFailedEventArgs(WebSocketError.Faulted));
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // Connection attempt was cancelled (likely due to timeout).
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs(WebSocketError.Timeout));
            }
            catch (WebSocketException exc)
            {
                Logger.Log($"Error during connection: {exc.Message}");
                // Report a connection failure based on the specific WebSocket error.
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs((WebSocketError)exc.WebSocketErrorCode));
            }
        }

        /// <summary>
        /// Attempts to establish a WebSocket connection within a specified timeout period.
        /// </summary>
        /// <param name="timeout">The maximum time (in milliseconds) allowed for the connection attempt.</param>
        /// <returns>A task representing the asynchronous connection attempt.</returns>
        public async Task AttemptConnect(int timeout)
        {
            // Only start a new connection if one isn't already in progress.
            if (_connectionTask == null || _connectionTask.Status != TaskStatus.Running)
            {
                // Prepare a cancellation token for the connection attempt.
                _connectionToken = new CancellationTokenSource();

                // Start the asynchronous connection process.
                _connectionTask = ConnectAsync(_connectionToken.Token);

                // Set up a timeout task that completes after the specified duration.
                CancellationTokenSource timeoutTokenSource = new CancellationTokenSource();
                Task timeoutTask = Task.Delay(timeout, timeoutTokenSource.Token);

                // Create a task that will complete when the connection either succeeds or fails.
                TaskCompletionSource<bool> connectionResult = new TaskCompletionSource<bool>();

                // Handler for successful connection.
                EventHandler onConnected = (s, a) =>
                {
                    connectionResult.SetResult(true);
                    timeoutTokenSource.Cancel();
                };

                // Handler for failed connection.
                EventHandler<ConnectionFailedEventArgs> onConnectionFailed = (s, a) =>
                {
                    connectionResult.SetResult(false);
                    timeoutTokenSource.Cancel();
                };

                // Subscribe to connection events.
                Connected += onConnected;
                ConnectionFailed += onConnectionFailed;

                // Wait for either the timeout or the connection result.
                Task completedTask = await Task.WhenAny(timeoutTask, connectionResult.Task);

                // If the timeout task finished first, cancel the connection attempt.
                if (completedTask == timeoutTask)
                {
                    _connectionToken.Cancel();
                }

                // Unsubscribe from the events.
                Connected -= onConnected;
                ConnectionFailed -= onConnectionFailed;
            }
        }

        /// <summary>
        /// Initiates a WebSocket connection attempt with the specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time (in milliseconds) to wait for a successful connection.</param>
        public void Connect(int timeout)
        {
            // Run the connection attempt asynchronously.
            Task.Run(() => AttemptConnect(timeout));
        }
        #endregion
    }
}
