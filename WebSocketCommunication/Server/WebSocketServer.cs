using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebSocketCommunication.Server
{
    public class WebSocketServer
    {
        #region Properties
        /// <summary>
        /// The base URL where the WebSocket server will be accessible.
        /// </summary>
        public string RootUrl { get; }

        /// <summary>
        /// The network port on which the WebSocket server listens for incoming connections.
        /// </summary>
        public ushort Port { get; }
        #endregion

        #region Fields
        /// <summary>
        /// The underlying web application that hosts the WebSocket server functionality.
        /// </summary>
        private WebApplication _application;

        /// <summary>
        /// Manager for handling and tracking active WebSocket connections.
        /// </summary>
        private WebSocketManager _connections = new();
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class with a specified root URL and port.
        /// </summary>
        /// <param name="rootUrl">The base URL for the server.</param>
        /// <param name="port">The port number on which the server listens.</param>
        public WebSocketServer(string rootUrl, ushort port)
        {
            // Create a new web application builder without pre-configured arguments.
            WebApplicationBuilder builder = WebApplication.CreateBuilder([]);

            // Configure the web host to listen on the specified URL and port.
            builder.WebHost.UseUrls($"http://{rootUrl}:{port}");

            // Remove default logging providers for custom logging configuration.
            builder.Logging.ClearProviders();

            // Add console-based logging to output diagnostic information.
            builder.Logging.AddConsole();

            // Set the logging threshold to capture debug-level events.
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            // Build the application and enable WebSocket support.
            _application = builder.Build();
            _application.UseWebSockets();

            // Assign the provided URL and port to the corresponding properties.
            RootUrl = rootUrl;
            Port = port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class using 'localhost' as the root URL.
        /// </summary>
        /// <param name="port">The port number on which the server listens.</param>
        public WebSocketServer(ushort port) : this("localhost", port) { }

        /// <summary>
        /// Registers a WebSocket service at the specified endpoint using a default-constructed handler.
        /// </summary>
        /// <typeparam name="TWebSocketHandler">The type of WebSocket handler.</typeparam>
        /// <param name="endpoint">The URL path at which to expose the WebSocket service.</param>
        public void AddService<TWebSocketHandler>(string endpoint) where TWebSocketHandler : WebSocketHandler
        {
            // Registers a WebSocket handler for the specified endpoint using the default constructor.
            AddService(endpoint, Activator.CreateInstance<TWebSocketHandler>);
        }

        /// <summary>
        /// Registers a WebSocket service at the specified endpoint using a factory to create the handler.
        /// </summary>
        /// <typeparam name="TWebSocketHandler">The type of WebSocket handler.</typeparam>
        /// <param name="endpoint">The URL path at which to expose the WebSocket service.</param>
        /// <param name="factory">A factory function to create instances of the WebSocket handler.</param>
        public void AddService<TWebSocketHandler>(string endpoint, Func<TWebSocketHandler> factory) where TWebSocketHandler : WebSocketHandler
        {
            // Maps the specified endpoint to the request handling function for incoming WebSocket connections.
            _application.Map(endpoint, (HttpContext context) => DirectWebSocketRequest(context, factory));
        }

        /// <summary>
        /// Handles a WebSocket request by creating a handler, accepting the connection, and attaching it.
        /// </summary>
        /// <typeparam name="TWebSocketHandler">The type of WebSocket handler to use.</typeparam>
        /// <param name="context">The HTTP context of the incoming request.</param>
        /// <param name="factory">A factory function to create the handler.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task DirectWebSocketRequest<TWebSocketHandler>(HttpContext context, Func<TWebSocketHandler> factory) where TWebSocketHandler : WebSocketHandler
        {
            // Check if the request is intended for WebSocket communication.
            if (!context.WebSockets.IsWebSocketRequest)
            {
                // Reject non-WebSocket requests by setting the status to "Method Not Allowed".
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            }
            else
            {
                // Create a new instance of the handler using the provided factory.
                WebSocketHandler handler = factory.Invoke();

                // Establish a new WebSocket connection and add it to the connection manager.
                ServerWebSocket webSocket = await _connections.Add(context);

                // Bind the new WebSocket connection to the handler.
                handler.Attach(webSocket, _connections);

                // Complete the WebSocket handshake and start communication.
                await webSocket.AcceptConnectionAsync();
            }
        }

        /// <summary>
        /// Runs the web application and begins processing requests synchronously.
        /// </summary>
        public void Run()
        {
            _application.Run();
        }

        /// <summary>
        /// Starts the web application asynchronously.
        /// </summary>
        public void Start()
        {
            _application.Start();
        }

        /// <summary>
        /// Asynchronously stops the web application.
        /// </summary>
        private async Task StopAsync()
        {
            await _connections.DisconnectAllAsync();
            await _application.StopAsync();
        }

        /// <summary>
        /// Stops the web application synchronously by awaiting the asynchronous stop operation.
        /// </summary>
        public void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }
        #endregion
    }
}
