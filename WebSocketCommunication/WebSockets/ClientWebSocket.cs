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
        ///
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
            if (_serverUrl.Scheme != Uri.UriSchemeWs)
            {
                throw new ArgumentException("Server uri scheme must be ws://", nameof(url));
            }
            InnerWebSocket = new SystemClientWebSocket();
        }

        /// <summary>
        /// Asynchronously initiates the connection to the WebSocket server.
        /// </summary>
        /// <param name="token">A token to observe while waiting for the connection to complete.</param>
        /// <returns>A task that represents the asynchronous connection operation.</returns>
        private async Task<WebSocketError> ConnectAsync(CancellationToken token)
        {
            await _connectionLock.WaitAsync();
            try
            {
                await InnerWebSocket.ConnectAsync(_serverUrl, token);
            }
            catch (OperationCanceledException)
            {
                InnerWebSocket = new SystemClientWebSocket();
                return WebSocketError.Timeout;
            }
            catch (WebSocketException exc)
            {
                InnerWebSocket = new SystemClientWebSocket();
                return (WebSocketError)exc.WebSocketErrorCode;
            }
            finally
            {
                _connectionLock.Release();
            }

            return WebSocketError.Success;
        }

        /// <summary>
        /// Attempts to establish a WebSocket connection within a specified timeout period.
        /// </summary>
        /// <param name="timeout">The maximum time (in milliseconds) allowed for the connection attempt.</param>
        /// <returns>A task representing the asynchronous connection attempt.</returns>
        public async Task AttemptConnect(int timeout)
        {
            // Create connection task
            CancellationTokenSource connectionTokenSource = new CancellationTokenSource();
            Task<WebSocketError> connectionTask = ConnectAsync(connectionTokenSource.Token);

            // Create timeout task
            CancellationTokenSource timeoutTokenSource = new CancellationTokenSource();
            Task timeoutTask = Task.Delay(timeout, timeoutTokenSource.Token);

            // Race the tasks
            Debug.Print("Race tasks");
            Task completedTask = await Task.WhenAny(timeoutTask, connectionTask);
            Debug.Print("Race finished");

            if (completedTask == timeoutTask)
            {
                Debug.Print("Timeout finished first");
                connectionTokenSource.Cancel();
            }
            else
            {
                Debug.Print("Connection finished first");
                timeoutTokenSource.Cancel();
            }

            Debug.Print("Await connection task");
            WebSocketError error = connectionTask.Result;
            Debug.Print("Connection task finished");
            switch (error)
            {
                case WebSocketError.Success:
                    RaiseConnectedEvent();
                    await ListenAsync();
                    break;

                case WebSocketError.Timeout:
                    RaiseConnectionFailedEvent(new ConnectionFailedEventArgs(WebSocketError.Timeout));
                    break;

                default:
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
            Task.Run(() => AttemptConnect(timeout));
        }
        #endregion
    }
}
