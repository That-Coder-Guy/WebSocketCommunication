using WebSocketCommunication.EventArguments;
using WebSocketCommunication.Server;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace TestServer
{
    public class Chat : WebSocketHandler
    {
        private static ConcurrentBag<string> _messages = new();

        protected override void OnConnected(object? sender, EventArgs e)
        {

        } 

        protected override void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
        {
            
        }

        protected override void OnDisconnected(object? sender, DisconnectEventArgs e)
        {

        }

        protected override void OnMessageReceived(object? sender, MessageEventArgs e)
        {
            Clients.Broadcast(e.Data.ToArray());
        }
    }
}
