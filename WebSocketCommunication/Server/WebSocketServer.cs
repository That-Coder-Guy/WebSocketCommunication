using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebSocketCommunication.WebSockets;

namespace WebSocketCommunication.Server
{
    public class WebSocketServer
    {

        #region Properties
        public string DomainName { get; }

        public ushort Port { get; }

        public bool IsListening => false; //_listener.IsListening;
        #endregion

        #region Fields
        private delegate WebSocketHandler WebSocketHandlerConstructor(ServerWebSocket webSocket);

        private WebApplication _application;

        private Dictionary<Uri, WebSocketHandlerConstructor> _webSocketHandlerMap { get; } = new();

        private WebSocketManager _connections = new();
        #endregion

        #region Public
        public WebSocketServer(string domain, ushort port)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder([]);
            builder.WebHost.UseUrls($"http://{domain}:{port}");

            _application = builder.Build();
            _application.UseWebSockets();

            DomainName = domain;
            Port = port;
        }

        public WebSocketServer(ushort port) : this("localhost", port) { }

        public void AddService<TWebSocketHandler>(string endpoint) where TWebSocketHandler : WebSocketHandler
        {
            _application.Map(endpoint, DirectWebSocketRequest<TWebSocketHandler>);
        }

        private async Task DirectWebSocketRequest<TWebSocketHandler>(HttpContext context) where TWebSocketHandler : WebSocketHandler
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                // Deny requests sent to unsupported endpoints.
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            }
            else
            {
                WebSocketHandler handler = Activator.CreateInstance<TWebSocketHandler>();
                ServerWebSocket webSocket = await _connections.Add(context);
                handler.Attach(webSocket, _connections);
                await webSocket.AcceptConnectionAsync();
            }
        }

        public void Run()
        {
            _application.Run();
        }

        public void Start()
        {
            _application.Start();
        }

        private async Task StopAsync()
        {
            await _application.StopAsync();
        }

        public void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }
        #endregion
    }
}
