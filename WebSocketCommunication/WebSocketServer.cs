using System.Diagnostics;
using System.Net;
using System.Reflection;
using Connection = System.Net.WebSockets.WebSocket;

namespace WebSocketCommunication
{
    public class WebSocketServer
    {

        #region Properties
        public string DomainName { get; }

        public ushort Port { get; }

        public bool IsListening => _listener.IsListening;
        #endregion

        #region Fields
        private HttpListener _listener = new HttpListener();

        private Task? _listenerTask;

        private Dictionary<Uri, Func<Connection, WebSocketHandler>> _webSocketHandlerMap { get; } = new();

        private List<Connection> _connections = [];
        #endregion

        #region Public Methods
        public WebSocketServer(string domain, ushort port)
        {
            DomainName = domain;
            Port = port;
        }

        public WebSocketServer(ushort port) : this("localhost", port) { }

        public void AddService<TWebSocketHandler>(string endpoint) where TWebSocketHandler : WebSocketHandler
        {
            endpoint = endpoint + (endpoint.EndsWith('/') ? "" : "/");
            string url = $"http://{DomainName}:{Port}/{endpoint}";
            _listener.Prefixes.Add(url);
            if (typeof(TWebSocketHandler).GetConstructor([typeof(Connection)]) is ConstructorInfo constructor)
            {
                _webSocketHandlerMap.Add(new Uri(url), (Connection connection) => (WebSocketHandler)constructor.Invoke([connection]));
            }
            else throw new MissingMethodException();
        }

        public void Start()
        {
            _listener.Start();
            _listenerTask = Task.Run(ListenAsync);
        }

        public void Stop()
        {
            _listener.Stop();
            _listenerTask?.Wait();
            // TODO: Close all open WebSocket connections
        }
        #endregion

        #region Private Methods
        private async Task ListenAsync()
        {
            while (_listener.IsListening)
            {
                try
                {
                    // Wait for an HTTP request.
                    HttpListenerContext context = await _listener.GetContextAsync();

                    // Process request
                    if (context.Request.IsWebSocketRequest)
                    {
                        // Upgrade connection type from HTTP to WebSocket.
                        Func<Connection, WebSocketHandler>? handler;
                        if (context.Request.Url is Uri endpoint && _webSocketHandlerMap.TryGetValue(endpoint, out handler))
                        {
                            // Accept web socket connection
                            Connection connection = (await context.AcceptWebSocketAsync(null)).WebSocket;
                            _connections.Add(connection);
                            handler.Invoke(connection);
                        }
                        else
                        {
                            // Deny requests sent to unsupported endpoints.
                            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                            context.Response.StatusDescription = "Method Not Allowed";
                            context.Response.Close();  // Close the connection
                        }
                    }
                    else
                    {
                        // Deny all request that are not HTTP to WebSocket upgrade requests.
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        context.Response.StatusDescription = "Method Not Allowed";
                        context.Response.Close();  // Close the connection
                    }
                }
                catch (HttpListenerException)
                {
                    // Gracefully close the listening process.
                    Debug.Print("Connection listening process has been terminated");
                }
            }
        }  
        #endregion
    }
}
