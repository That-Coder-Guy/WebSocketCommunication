using System.ComponentModel.Design.Serialization;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace WebSocketCommunication
{
    public class WebSocketServer : IDisposable
    {

        #region Propertiesx
        public string DomainName { get; }

        public ushort Port { get; }

        public bool IsListening => _listener.IsListening;

        public IEnumerable<WebSocket> Connections => _connections.Keys.AsEnumerable();
        #endregion

        #region Fields
        private HttpListener _listener = new HttpListener();

        private CancellationTokenSource? _listenerToken;

        private Dictionary<Uri, Func<WebSocket, WebSocketHandler>> _webSocketHandlerMap { get; } = new();

        private Dictionary<WebSocket, WebSocketHandler> _connections = new();
        #endregion

        #region Public Methods
        public WebSocketServer(string domain, ushort port)
        {
            DomainName = domain;
            Port = port;
        }

        public WebSocketServer(ushort port) : this("localhost", port) { }

        public void AddService<TWebSocketHandler>(string endpoint) where TWebSocketHandler : WebSocketHandler, new()
        {
            endpoint = endpoint + (endpoint.EndsWith('/') ? "" : "/");
            string url = $"http://{DomainName}:{Port}/{endpoint}";
            _listener.Prefixes.Add(url);
            _webSocketHandlerMap.Add(new Uri(url), (WebSocket webSocket) => new TWebSocketHandler());
        }

        public void Start()
        {
            _listener.Start();
            Listen();
        }

        public void Stop()
        {
            Deafen();
            _listener.Stop();
        }

        public void Dispose()
        {
            Deafen();
            _listener.Close();
        }
        #endregion

        #region Private Methods
        private async Task ListenAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Task? completedTask = await Task.WhenAny(_listener.GetContextAsync(), Task.Delay(-1, token));
                    if (completedTask is Task<HttpListenerContext> contextTask)
                    {
                        HttpListenerContext context = await contextTask;
                        if (context.Request.IsWebSocketRequest)
                        {
                            Func<WebSocketHandler>? handler;
                            if (context.Request.Url is Uri endpoint && _webSocketHandlerMap.TryGetValue(endpoint, out handler))
                            {
                                _connections.Add((await context.AcceptWebSocketAsync(null)).WebSocket, handler.Invoke());
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { /* Listening ended */ }
            }
        }

        private void Listen()
        {
            _listenerToken = new CancellationTokenSource();
            Task listenTask = Task.Run(() => ListenAsync(_listenerToken.Token));
        }

        private void Deafen()
        {
            if (_listenerToken != null)
            {
                _listenerToken?.Cancel();
                _listenerToken?.Dispose();
            }
        }
        #endregion
    }
}
