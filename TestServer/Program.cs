using TestServer;
using WebSocketCommunication.Server;

WebSocketServer server = new WebSocketServer(8080);
server.AddService<Chat>("/chat/");
server.Start();
Console.ReadKey();