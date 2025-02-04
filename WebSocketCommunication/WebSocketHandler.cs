using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using Connection = System.Net.WebSockets.WebSocket;

namespace WebSocketCommunication
{
    public abstract class WebSocketHandler
    {
        private WebSocket _webSocket;

        internal WebSocketHandler(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }
    }
}
