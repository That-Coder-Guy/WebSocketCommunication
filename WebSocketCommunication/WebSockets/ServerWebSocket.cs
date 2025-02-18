using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Net.WebSockets;
using System.Text;
using SystemWebSocket = System.Net.WebSockets.WebSocket;

namespace WebSocketCommunication
{
    /// <summary>
    /// Represents a server-side WebSocket connection with a client.
    /// </summary>
    public class ServerWebSocket : WebSocket<SystemWebSocket>
    {
        #region Fields
        /// <summary>
        /// Holds the HTTP context associated with the WebSocket upgrade request.
        /// </summary>
        private HttpContext _context;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the underlying WebSocket instance.
        /// </summary>
        protected override SystemWebSocket InnerWebSocket { get; set; }

        /// <summary>
        /// Gets the unique identifier for this WebSocket connection.
        /// </summary>
        public string Id { get; private set; } = string.Empty;
        #endregion

        #region Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerWebSocket"/> class.
        /// </summary>
        /// <param name="context">The HTTP context for the WebSocket upgrade request.</param>
        public ServerWebSocket(HttpContext context)
        {
            // Create a dummy WebSocket instance since the connection is not yet established.
            InnerWebSocket = SystemWebSocket.CreateFromStream(Stream.Null, false, null, TimeSpan.Zero);
            _context = context;
        }

        /// <summary>
        /// Asynchronously accepts an incoming WebSocket connection.
        /// </summary>
        /// <returns>A task representing the asynchronous accept operation.</returns>
        public async Task AcceptConnectionAsync()
        {
            try
            {
                // Accept the incoming WebSocket connection.
                InnerWebSocket = await _context.WebSockets.AcceptWebSocketAsync();

                // Generate a unique identifier using the remote IP, port, and user agent.
                string? ipAddress = _context.Connection.RemoteIpAddress?.ToString();
                string port = _context.Connection.RemotePort.ToString();
                string userAgent = _context.Request.Headers["User-Agent"].ToString();

                string idString = $"{ipAddress}-{port}-{userAgent}";

                // Compute a SHA256 hash of the identifier string to create a unique connection ID.
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(idString));
                    Id = Convert.ToBase64String(hashBytes);
                }

                // Trigger the event to notify that the connection has been established.
                RaiseConnectedEvent();

                // Begin listening for incoming messages on this connection.
                await ListenAsync();
            }
            catch (WebSocketException exc)
            {
                // Trigger the connection failure event with the relevant error information.
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs((WebSocketError)exc.WebSocketErrorCode));
            }
        }

        /// <summary>
        /// Initiates the asynchronous acceptance of an incoming WebSocket connection.
        /// </summary>
        public void AcceptConnection()
        {
            Task.Run(AcceptConnectionAsync);
        }
        #endregion
    }
}
