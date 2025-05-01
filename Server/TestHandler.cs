using System.Text;
using WebSocketCommunication;
using WebSocketCommunication.Server;

namespace Server
{
    public class TestHandler : WebSocketHandler
    {
        protected override void OnConnected(object? sender, EventArgs e)
        {
            Console.WriteLine($"Connected");
        }
        protected override void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
        {
            Console.WriteLine("Connection Failed");
        }
        protected override void OnMessageReceived(object? sender, MessageEventArgs e)
        {
            Console.WriteLine($"Message Received: {Encoding.UTF8.GetString(e.Data.ToArray())}");
            Send(Encoding.UTF8.GetBytes("Hello I am a server"));
        }
        protected override void OnDisconnected(object? sender, DisconnectEventArgs e)
        {
            Console.WriteLine($"Disconnected ({e.Reason})");
        }
    }
}
