using System.Diagnostics;
using System.Net.WebSockets;
using WebSocketCommunication.Utilities;
using SystemWebSocket = System.Net.WebSockets.WebSocket;
using SystemWebSocketState = System.Net.WebSockets.WebSocketState;

namespace WebSocketCommunication.Communication
{
    public class WebSocket
    {
        #region Fields
        private readonly uint BUFFER_SIZE = 16384;

        private static uint NextWebSocketId = 0u;

        private SystemWebSocket _innerWebSocket;

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
        
        public WebSocketState State => (WebSocketState)_innerWebSocket.State;

        public uint Id { get; } = NextWebSocketId++;
        #endregion

        #region Methods
        internal WebSocket(SystemWebSocket webSocket)
        {
            _innerWebSocket = webSocket;
        }

        public WebSocket(string url)
        {
            _serverUrl = new Uri(url);
            _innerWebSocket = new ClientWebSocket();
        }

        public void BeginConnect()
        {
            // If the connection task has never been run or is done running
            if (_connectionTask == null || _connectionTask.IsCompleted)
            {
                // Create a cancellation token for the connection task
                _connectionToken = new CancellationTokenSource();

                // Start the conenction task
                _connectionTask = Task.Run(() => ConnectAsync(_connectionToken.Token));
            }
        }

        public async Task ConnectAsync(CancellationToken token)
        {
            if (_serverUrl is Uri url && _innerWebSocket is ClientWebSocket client)
            {
                try
                {
                    await client.ConnectAsync(url, token);
                    switch (client.State)
                    {
                        case SystemWebSocketState.Open:
                            BeginListening();
                            Connected?.Invoke();
                            break;
                        case SystemWebSocketState.Closed:
                            Disconnected?.Invoke();
                            break;
                        default:
                            Debug.Print($"Unexpected WebSocket State: {client.State}");
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.Print($"WebSocket listening task has been terminated.");
                }
            }
            else throw new InvalidOperationException("Not a client WebSocket");
        }

        /// <summary>
        /// Ends connection attempt if currently running
        /// </summary>
        /// <returns>Whether the connection process was ended</returns>
        public bool EndConnect()
        {
            return false;
        }


        public void Connect()
        {
            BeginConnect();
            while (_innerWebSocket.State == SystemWebSocketState.Connecting)
            {
                Task.Delay(1000).Wait();
            }
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
                while (_innerWebSocket.State == SystemWebSocketState.Open)
                {
                    // Create a memory stream to store the incoming data
                    using (MemoryStream data = new MemoryStream())
                    {
                        // Loop to reaceive the full message through the buffer
                        WebSocketReceiveResult result;
                        do
                        {
                            // Wait to receive data for mthe web socket
                            result = await _innerWebSocket.ReceiveAsync(buffer, token);

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
                // TODO: Determine if needed
                // If the web socket is closing
                if (_innerWebSocket.State != SystemWebSocketState.Closed)
                {
                    await _innerWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
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
            await _innerWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        #endregion
    }
}
