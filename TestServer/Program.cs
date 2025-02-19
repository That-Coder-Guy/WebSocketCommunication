using TestServer;
using WebSocketCommunication.Server;

WebSocketServer server = new WebSocketServer("127.0.0.1", 8080);
server.AddService<Chat>("/chat/");
server.Run();