using TestServer;
using WebSocketCommunication.Server;

WebSocketServer server = new WebSocketServer("0.0.0.0", 8080, null);
server.AddService<Chat>("/chat/");
server.Run();