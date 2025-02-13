using SystemClientWebSocket = System.Net.WebSockets.ClientWebSocket;
using SystemWebSocketState = System.Net.WebSockets.WebSocketState;
using System.Diagnostics;
using WebSocketCommunication.EventArguments;
using WebSocketException = System.Net.WebSockets.WebSocketException;
using WebSocketCommunication.Enumerations;
using WebSocketCommunication.Logging;
using System.Threading.Tasks;

namespace WebSocketCommunication.WebSockets
{
    /// <summary>
    /// Represents a client's web socket connection to a server.
    /// </summary>
    public class ClientWebSocket : WebSocket<SystemClientWebSocket>
    {
        #region Fields
        private Uri _serverUrl;
        #endregion

        #region Properties
        protected override SystemClientWebSocket InnerWebSocket { get; set; }
        #endregion

        #region Methods
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
            try
            {
                await InnerWebSocket.ConnectAsync(_serverUrl, token);
                switch (InnerWebSocket.State)
                {
                    case SystemWebSocketState.Open:
                        Logger.Log("Connection succeeded!");
                        BeginListening();
                        RaiseConnectedEvent();
                        break;
                    default:
                        Logger.Log($"Unexpected WebSocket state ({InnerWebSocket.State})");
                        await DisconnectAsync();
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log("Connection attempt cancelled.");
            }
            catch (WebSocketException exc)
            {
                Logger.Log($"Connection attempt failed with ({exc.WebSocketErrorCode})");
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs((WebSocketError)exc.WebSocketErrorCode));
            }
            catch (Exception exc)
            {
                Logger.Log($"{exc.Message}");
            }
        }

        /// <summary>
        /// Starts a connection attempt if not currently running.
        /// </summary>
        /// <returns>Whether the connection process was started.</returns>
        public bool BeginConnect()
        {
            // If the connection task has never been run or is done running
            if (!IsTaskRunning(_connectionTask))
            {
                Logger.Log("Attempting to connect to host...");

                // Create a cancellation token for the connection task
                _connectionToken = new CancellationTokenSource();

                // Start the conenction task
                _connectionTask = Task.Run(() => ConnectAsync(_connectionToken.Token));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Ends a connection attempt if currently running.
        /// </summary>
        /// <returns>Whether the connection process was ended.</returns>
        public bool EndConnect()
        {
            Logger.Log("Cancelling connection attempt...");

            if (_connectionTask != null && _connectionTask.Status == TaskStatus.Running)
            {
                _connectionToken.Cancel();
                _connectionTask.Wait();
                return _connectionTask.IsCanceled;
            }
            return false;
        }

        /// <summary>
        /// Attempts to connect attempt if not currently running.
        /// </summary>
        /// <returns>Whether the connection process succeeded.</returns>
        public bool Connect()
        {
            if (!BeginConnect())
            {
                Task.Delay(10000).Wait();
                if (InnerWebSocket.State == SystemWebSocketState.Connecting)
                {
                    EndConnect();
                    return false;
                }
                return true;
            }
            else throw new InvalidOperationException("Connection task already in progress.");
        }
        #endregion
    }
}
