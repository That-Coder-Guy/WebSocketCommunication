using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.WebSockets;
using WebSocketCommunication.EventArguments;
using SystemWebSocket = System.Net.WebSockets.WebSocket;
using WebSocketError = WebSocketCommunication.Enumerations.WebSocketError;

namespace WebSocketCommunication.WebSockets
{
    public class ServerWebSocket : WebSocket<SystemWebSocket>
    {
        #region Fields
        private static uint NextWebSocketId = 0u;

        private HttpListenerContext _context;
        #endregion

        #region Properties
        protected override SystemWebSocket InnerWebSocket { get; set; }
        
        public uint Id { get; } = NextWebSocketId++;
        #endregion

        #region Methods
        public ServerWebSocket(HttpListenerContext context)
        {
            InnerWebSocket = SystemWebSocket.CreateFromStream(Stream.Null, false, null, TimeSpan.Zero);  // False connection
            _context = context;
        }

        public async Task AcceptConnectionAsync()
        {
            try
            {
                InnerWebSocket = (await _context.AcceptWebSocketAsync(null)).WebSocket;
                BeginListening();
                RaiseConnectedEvent();
            }
            catch (WebSocketException exc)
            {
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs((WebSocketError)exc.WebSocketErrorCode));
            }
        }
        
        public void AcceptConnection()
        {
            Task.Run(AcceptConnectionAsync);
        }
        #endregion
    }
}
