using System.Net;
using System.Net.WebSockets;
using SystemWebSocket = System.Net.WebSockets.WebSocket;
using SystemWebSocketState = System.Net.WebSockets.WebSocketState;

namespace WebSocketCommunication.Communication
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

        protected override async Task ConnectAsync(CancellationToken token)
        {
            InnerWebSocket = (await _context.AcceptWebSocketAsync(null)).WebSocket;
            RaiseConnectedEvent();
        }
        #endregion
    }
}
