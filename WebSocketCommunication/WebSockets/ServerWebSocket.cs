using System.Net;
using System.Net.WebSockets;
using WebSocketCommunication.EventArguments;
using SystemWebSocket = System.Net.WebSockets.WebSocket;
using WebSocketError = WebSocketCommunication.Enumerations.WebSocketError;

namespace WebSocketCommunication.WebSockets
{
    /// <summary>
    /// Represents a server's web socket connection to a client.
    /// </summary>
    public class ServerWebSocket : WebSocket<SystemWebSocket>
    {
        #region Fields
        /// <summary>
        /// The identifier for the next web socket connection.
        /// </summary>
        private static uint NextWebSocketId = 0u;

        /// <summary>
        /// The context handle to the websocket update request.
        /// </summary>
        private HttpListenerContext _context;
        #endregion

        #region Properties
        protected override SystemWebSocket InnerWebSocket { get; set; }
        
        /// <summary>
        /// The identifier of the web socket connection.
        /// </summary>
        public uint Id { get; } = NextWebSocketId++;
        #endregion

        #region Methods
        /// <summary>
        /// The ServerWebSocket constructor.
        /// </summary>
        /// <param name="context">The context handle to the websocket update request being realized.</param>
        public ServerWebSocket(HttpListenerContext context)
        {
            InnerWebSocket = SystemWebSocket.CreateFromStream(Stream.Null, false, null, TimeSpan.Zero);  // False connection
            _context = context;
        }
        
        /// <summary>
        /// Accepts an incoming web socket connection as an asynchronous operation.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task AcceptConnectionAsync()
        {
            try
            {
                // Attempt to accept the connection
                InnerWebSocket = (await _context.AcceptWebSocketAsync(null)).WebSocket;

                // Start the message listening task
                BeginListening();

                // Invoke the connected event
                RaiseConnectedEvent();
            }
            catch (WebSocketException exc)
            {
                // In voke the connection fail event if an exception occures
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs((WebSocketError)exc.WebSocketErrorCode));
            }
        }

        /// <summary>
        /// Accepts an incoming web socket connection.
        /// </summary>
        public void AcceptConnection()
        {
            Task.Run(AcceptConnectionAsync);
        }
        #endregion
    }
}
