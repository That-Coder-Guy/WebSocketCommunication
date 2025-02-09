using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using WebSocketCommunication.Enumerations;
using WebSocketCommunication.EventArguments;
using SystemWebSocket = System.Net.WebSockets.WebSocket;
using SystemWebSocketState = System.Net.WebSockets.WebSocketState;

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
                switch (exc.WebSocketErrorCode)
                {
                    case WebSocketError.ConnectionClosedPrematurely:
                        RaiseDisconnectedEvent(new DisconnectEventArgs(WebSocketClosureReason.EndpointUnavailable));
                        break;
                    case WebSocketError.NotAWebSocket:
                        RaiseDisconnectedEvent(new DisconnectEventArgs(WebSocketClosureReason.ProtocolError));
                        break;
                    default:
                        RaiseDisconnectedEvent(new DisconnectEventArgs(WebSocketClosureReason.Empty));
                        Debug.Print($"{exc}");
                        break;
                }
            }
        }
        
        public void AcceptConnection()
        {
            Task.Run(AcceptConnectAsync);
        }
        #endregion
    }
}
