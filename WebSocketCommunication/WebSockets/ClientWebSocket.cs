using WebSocketException = System.Net.WebSockets.WebSocketException;
using SystemClientWebSocket = System.Net.WebSockets.ClientWebSocket;
using SystemWebSocketState = System.Net.WebSockets.WebSocketState;
using WebSocketCommunication.EventArguments;
using WebSocketCommunication.Enumerations;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WebSocketCommunication.WebSockets
{
    /// <summary>
    /// Represents a client's web socket connection to a server.
    /// </summary>
    public class ClientWebSocket : WebSocket<SystemClientWebSocket>
    {
        #region Fields
        /// <summary>
        /// The uniform resource identifier of the server to be connected to.
        /// </summary>
        private Uri _serverUrl;
        #endregion

        #region Properties
        protected override SystemClientWebSocket InnerWebSocket { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// The ClientWebSocket constructor.
        /// </summary>
        /// <param name="url">The URL of the server to connect to.</param>
        public ClientWebSocket(string url)
        {
            _serverUrl = new Uri(url);
            InnerWebSocket = new SystemClientWebSocket();
        }

        /// <summary>
        /// Starts the connection process with a web socket server as an asynchronous operation.
        /// </summary>
        /// <param name="token">A cancellation token used to propagate notification that the operation should be canceled.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private async Task ConnectAsync(CancellationToken token)
        {
            Logger.Log("Starting connection process...");
            try
            {
                await InnerWebSocket.ConnectAsync(_serverUrl, token);
                switch (InnerWebSocket.State)
                {
                    case SystemWebSocketState.Open:
                        Logger.Log($"Connection process succeeded");
                        RaiseConnectedEvent();
                        await ListenAsync();
                        break;
                    default:
                        Logger.Log($"Connection process failed");
                        RaiseConnectionFailedEvent(new ConnectionFailedEventArgs(WebSocketError.Faulted));
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // Connection attempt cancelled
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs(WebSocketError.Timeout));
            }
            catch (WebSocketException exc)
            {
                Logger.Log($"Error occured during connection ({exc.Message})");
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs((WebSocketError)exc.WebSocketErrorCode));
            }
        }

        /// <summary>
        /// Attempts to create a web socket connection within a certain amout of time as an asynchronous operation.
        /// </summary>
        /// <param name="timeout">The amount of time which the connection attempt is allotted.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task AttemptConnect(int timeout)
        {
            if (_connectionTask == null || _connectionTask.Status != TaskStatus.Running)
            {
                // Create a cancellation token for the connection task
                _connectionToken = new CancellationTokenSource();

                // Create connection task
                _connectionTask = ConnectAsync(_connectionToken.Token);

                // Create a timeout task
                CancellationTokenSource timeoutTokenSource = new CancellationTokenSource();
                Task timeoutTask = Task.Delay(timeout, timeoutTokenSource.Token);

                // Create a task that completes when the web socket connection attempt succeeds or fails
                TaskCompletionSource<bool> connectionSucceeded = new TaskCompletionSource<bool>();
                
                EventHandler onConnected = (s, a) =>
                {
                    connectionSucceeded.SetResult(true);
                    timeoutTokenSource.Cancel();
                };
                EventHandler<ConnectionFailedEventArgs> onConnectionFailed = (s, a) =>
                {
                    connectionSucceeded.SetResult(false);
                    timeoutTokenSource.Cancel();
                };
                
                Connected += onConnected;
                ConnectionFailed += onConnectionFailed;

                // Wait to see which task finishes first
                Task? completedTask = await Task.WhenAny(timeoutTask, connectionSucceeded.Task);

                // Cancel the web socket connection attempt if the timeout task finished first
                if (completedTask == timeoutTask)
                {
                    _connectionToken.Cancel();
                }

                // Clean up
                Connected -= onConnected;
                ConnectionFailed -= onConnectionFailed;
            }
        }

        /// <summary>
        /// Attempts to create a web socket connection within a certain amout of time.
        /// </summary>
        /// // <param name="timeout">The amount of time which the connection attempt is allotted.</param>
        public void Connect(int timeout)
        {
            Task.Run(() => AttemptConnect(timeout));
        }
        #endregion
    }
}
