using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using WebSocketCommunication.EventArguments;
using SystemWebSocket = System.Net.WebSockets.WebSocket;
using WebSocketError = WebSocketCommunication.Enumerations.WebSocketError;

namespace WebSocketCommunication.WebSockets
{
    internal class ServerWebSocket : WebSocket<SystemWebSocket>
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
        internal ServerWebSocket(HttpListenerContext context)
        {
            InnerWebSocket = SystemWebSocket.CreateFromStream(Stream.Null, false, null, TimeSpan.Zero);  // False connection
            _context = context;
        }

        private async Task AcceptConnectAsync()
        {
            try
            {
                InnerWebSocket = (await _context.AcceptWebSocketAsync(null)).WebSocket;
                RaiseConnectedEvent();
            }
            catch (WebSocketException exc)
            {
                RaiseConnectionFailedEvent(new ConnectionFailedEventArgs((WebSocketError)exc.WebSocketErrorCode));
                Debug.Print($"{exc}");
            }
        }
        
        public void AcceptConnection()
        {
            Task.Run(AcceptConnectAsync);
        }
        #endregion
    }
}
