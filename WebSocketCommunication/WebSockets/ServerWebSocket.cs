using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Net.WebSockets;
using System.Text;
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
        /// The context handle to the websocket update request.
        /// </summary>
        private HttpContext _context;
        #endregion

        #region Properties
        protected override SystemWebSocket InnerWebSocket { get; set; }
        
        /// <summary>
        /// The identifier of the web socket connection.
        /// </summary>
        public string Id { get; private set; } = string.Empty;
        #endregion

        #region Methods
        /// <summary>
        /// The ServerWebSocket constructor.
        /// </summary>
        /// <param name="context">The context handle to the websocket update request being realized.</param>
        public ServerWebSocket(HttpContext context)
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
            Logger.Log("Starting connection accepting process...");
            try
            {
                // Attempt to accept the connection
                InnerWebSocket = await _context.WebSockets.AcceptWebSocketAsync();
                Logger.Log($"Connection accepted successfully");

                // Create a unique identifier for the web socket connection
                string? ipAddress = _context.Connection.RemoteIpAddress?.ToString();
                string port = _context.Connection.RemotePort.ToString();
                string userAgent = _context.Request.Headers["User-Agent"].ToString();

                string idString = $"{ipAddress}-{port}-{userAgent}";

                // Generate a hash of the combined string to create a unique identifier
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(idString));
                    Id = Convert.ToBase64String(hashBytes);
                }

                // Invoke the connected event
                RaiseConnectedEvent();

                // Listen for messages
                await ListenAsync();
            }
            catch (WebSocketException exc)
            {
                Logger.Log($"Connection unsuccessful ({exc.Message})");
                // Invoke the connection fail event if an exception occures
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
