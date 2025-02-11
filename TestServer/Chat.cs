using WebSocketCommunication.EventArguments;
using WebSocketCommunication.Server;
using WebSocketCommunication.WebSockets;

namespace TestServer
{
    public class Chat : WebSocketHandler
    {
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

        }
    }
}
