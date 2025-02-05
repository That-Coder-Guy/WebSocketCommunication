using System.Diagnostics;
using System.Net.WebSockets;
using WebSocketCommunication.Utilities;
using SystemWebSocket = System.Net.WebSockets.WebSocket;

namespace WebSocketCommunication.Communication
{
    public class WebSocket
    {
        #region Fields
        private readonly uint BUFFER_SIZE = 16384;

        private static uint NextWebSocketId = 0u;

        private SystemWebSocket _webSocket;

        private Uri? _serverUrl;

        private Task? _messageListenerTask;

        private CancellationTokenSource _messageListenerToken = new();

        private Task? _connectionTask;

        private CancellationTokenSource _connectionToken = new();
        #endregion

        #region Events
        public event Action? Connected;

        public event EventHandler<MessageEventArgs>? MessageReceived;

        public event Action? Disconnected;
        #endregion

        #region Properties
        public uint Id { get; } = NextWebSocketId++;
        #endregion

        #region Methods
        /*
        internal WebSocket(SystemWebSocket webSocket)
        {
            _webSocket = webSocket;
        }
        */

        public WebSocket(string url)
        {
            _serverUrl = new Uri(url);
            _webSocket = new ClientWebSocket();
        }

        public async Task BeginConnectAsync()
        {

        }

        public async Task ConnectAsync(CancellationToken token)
        {
            if (_serverUrl is Uri url && _webSocket is ClientWebSocket clientWebSocket)
            {
                try
                {
                    await clientWebSocket.ConnectAsync(url, token);
                    if (clientWebSocket.State == WebSocketState.Open)
                    {
                        BeginListening();
                        Connected?.Invoke();
                    }
                    else throw new WebSocketException();
                }
                catch (OperationCanceledException)
                {

                }
            }
            else throw new InvalidOperationException();
        }

        public async Task EndConnectAsync()
        {

        }

        public void Connect() // Goofy ahh synchronous method
        {
            BeginConnectAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Starts message listener thread if it is not already running.
        /// </summary>
        private void BeginListening()
        {
            // If the listening task is not running
            if (_messageListenerTask == null || _messageListenerTask.IsCompleted)
            {
                // Create a cancellation token for the listening task
                _messageListenerToken = new CancellationTokenSource();

                // Start the listening task
                _messageListenerTask = Task.Run(() => ListenAsync(_messageListenerToken.Token));
            }
        }
        /// <summary>
        /// Listens for messages while the web socket is open.
        /// </summary>
        /// <param name="token">The means to cancel the listening task</param>
        /// <returns>Generic task</returns>
        public async Task ListenAsync(CancellationToken token)
        {
            try
            {
                // Create a input buffer
                byte[] buffer = new byte[BUFFER_SIZE];

                // Listen while the connection is open
                while (_webSocket.State == WebSocketState.Open)
                {
                    // Create a memory stream to store the incoming data
                    using (MemoryStream data = new MemoryStream())
                    {
                        // Loop to reaceive the full message through the buffer
                        WebSocketReceiveResult result;
                        do
                        {
                            // Wait to receive data for mthe web socket
                            result = await _webSocket.ReceiveAsync(buffer, token);

                            // Add the buffer to the total data memory stream
                            data.Write(buffer, 0, result.Count);
                        }
                        while (!result.EndOfMessage);

                        // Raise the message received event
                        MessageReceived?.Invoke(this, new MessageEventArgs(data));
                    }
                }
            }
            catch (WebSocketException ex)
            {
                // If the web socket throws an error
                Debug.Print($"WebSocket error: {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                // If the listening task is canceled
                Debug.Print("Listening process canceled.");
            }
            finally
            {
                // If the web socket is closing
                if (_webSocket.State != WebSocketState.Closed)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// Stops the message listener thread if it is running.
        /// </summary>
        private void EndListening()
        {
            // If the listening task has been started
            if (_messageListenerTask != null)
            {
                // Request cancellation of the listening task
                _messageListenerToken.Cancel();

                // Wait for listening task to end
                _messageListenerTask.Wait();
            }
        }


        public void Disconnect() // Goofy ahh synchronous method
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }

        public async Task DisconnectAsync()
        {
            EndListening();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        #endregion
    }
}
