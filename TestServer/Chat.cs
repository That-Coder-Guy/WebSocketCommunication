using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketCommunication.EventArguments;
using WebSocketCommunication.Server;

namespace TestServer
{
    public class Chat : WebSocketHandler
    {
        public override void OnConnected()
        {
            throw new NotImplementedException();
        }

        public override void OnDisconnected()
        {
            throw new NotImplementedException();
        }

        public override void OnMessageReceived(object? sender, MessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
