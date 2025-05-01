using System.Text;
using WebSocketCommunication;

ClientWebSocket client = new ClientWebSocket("ws://10.130.160.117:8080/test/");

client.Connected += OnConnected;
client.ConnectionFailed += OnConnectionFailed;
client.MessageReceived += OnMessageReceived;
client.Disconnected += OnDisconnected;

client.Connect(1000);
Console.ReadKey();
client.Disconnect();

void OnConnected(object? sender, EventArgs e)
{
    Console.WriteLine("Connected");
    client.Send(Encoding.UTF8.GetBytes("Hello I am a client"));
}
void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
{
    Console.WriteLine("Connection Failed");
}
void OnMessageReceived(object? sender, MessageEventArgs e)
{
    Console.WriteLine($"Message Received: {Encoding.UTF8.GetString(e.Data.ToArray())}");
}
void OnDisconnected(object? sender, DisconnectEventArgs e)
{
    Console.WriteLine("Connected");
}
