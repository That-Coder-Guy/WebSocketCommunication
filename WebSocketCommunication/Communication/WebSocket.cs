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
            if (_serverUrl is Uri url && _webSocket is ClientWebSocket clientWebSocket)
            {
                await clientWebSocket.ConnectAsync(url, CancellationToken.None);
                if (clientWebSocket.State == WebSocketState.Open)
                {
                    _messageListenerTask = Task.Run(BeginListeningAsync);
                    Connected?.Invoke();
                }
                else throw new WebSocketException();
            }
            else throw new WebSocketException();
        }

        public async Task EndConnectAsync()
        {

        }

        public void Connect() // Goofy ahh synchronous method
        {
            ConnectAsync().GetAwaiter().GetResult();
        }

        private async Task BeginListeningAsync()
        {
            _messageListenerTask = ListenAsync(_messageListenerToken.Token);
            await _messageListenerTask;
        }

        private async Task ListenAsync(CancellationToken token)
        {
            try
            {
                byte[] buffer = new byte[BUFFER_SIZE];

                while (_webSocket.State == WebSocketState.Open)
                {
                    using (MemoryStream data = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await _webSocket.ReceiveAsync(buffer, token);
                            data.Write(buffer, 0, result.Count);
                        }
                        while (!result.EndOfMessage);

                        MessageReceived?.Invoke(this, new MessageEventArgs(data));
                    }
                }
            }
            catch (WebSocketException ex)
            {
                Debug.Print($"WebSocket error: {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                Debug.Print("Listening canceled.");
            }
            finally
            {
                if (_webSocket.State != WebSocketState.Closed)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        }

        private async Task EndListeningAsync()
        {
            _messageListenerToken.Cancel();
            if (_messageListenerTask != null)
            {
                await _messageListenerTask;
            }
        }


        public void Disconnect() // Goofy ahh synchronous method
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }

        public async Task DisconnectAsync()
        {
            await EndListeningAsync();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        #endregion
    }
}
