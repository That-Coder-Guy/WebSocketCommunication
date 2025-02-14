﻿using System.Diagnostics;
using System.Net;
using WebSocketCommunication.WebSockets;

namespace WebSocketCommunication.Server
{
    public class WebSocketServer
    {

        #region Properties
        public string DomainName { get; }

        public ushort Port { get; }

        public bool IsListening => _listener.IsListening;
        #endregion

        #region Fields
        private delegate WebSocketHandler WebSocketHandlerConstructor(ServerWebSocket webSocket);

        private HttpListener _listener = new HttpListener();

        private Task? _listenerTask;

        private Dictionary<Uri, WebSocketHandlerConstructor> _webSocketHandlerMap { get; } = new();

        private WebSocketManager _connections = new();
        #endregion

        #region Public
        public WebSocketServer(string domain, ushort port)
        {
            DomainName = domain;
            Port = port;
        }

        public WebSocketServer(ushort port) : this("localhost", port) { }

        public void AddService<TWebSocketHandler>(string endpoint) where TWebSocketHandler : WebSocketHandler
        {
            endpoint = (endpoint.StartsWith('/') ? "" : "/") + endpoint + (endpoint.EndsWith('/') ? "" : "/");
            string url = $"http://{DomainName}:{Port}{endpoint}";
            Debug.Print(url);
            _listener.Prefixes.Add(url);
            _webSocketHandlerMap.Add(new Uri(url), (webSocket) =>
            {
                WebSocketHandler handler = Activator.CreateInstance<TWebSocketHandler>();
                Task.Run(() => handler.Attach(webSocket, _connections));
                return handler;
            });
        }

        public void Start()
        {
            try
            {
                _listener.Start();
                _listenerTask = Task.Run(ListenAsync);
            }
            catch (HttpListenerException exc)
            {
                Debug.Print($"{exc.Message}");
            }
        }

        public void Stop()
        {
            _listener.Stop();
            _listenerTask?.Wait();
            _connections.DisconnectAll();
        }

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
                        WebSocketHandlerConstructor? handler;
                        if (context.Request.Url is Uri endpoint && _webSocketHandlerMap.TryGetValue(endpoint, out handler))
                        {
                            // Accept web socket connection
                            ServerWebSocket webSocket = await _connections.Add(context);
                            handler.Invoke(webSocket);
                            await webSocket.AcceptConnectionAsync();
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
                }
            }
        }
        #endregion
    }
}
