using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Connection = System.Net.WebSockets.WebSocket;

namespace WebSocketCommunication
{
    public class WebSocket
    {
        private Connection _socket;

        internal WebSocket(Connection socket)
        {
            _socket = socket;
            
        }
    }
}
