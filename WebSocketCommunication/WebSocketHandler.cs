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
        private Connection _connection;

        internal WebSocketHandler(Connection connection)
        {
            _connection = connection;
        }
    }
}
