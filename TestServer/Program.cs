using TestServer;
using WebSocketCommunication.Server;

WebSocketServer server = new WebSocketServer("0.0.0.0", 8080,"logs/server.log");
server.AddService<Chat>("/chat/");
server.Start();
Console.ReadKey();