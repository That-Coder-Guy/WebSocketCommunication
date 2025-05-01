using Server;
using WebSocketCommunication.Server;

WebSocketServer server = new WebSocketServer("10.130.160.117", 8080);

server.AddService<TestHandler>("/test/");

server.Start();
Console.ReadKey();
server.Stop();