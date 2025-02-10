using WebSocketCommunication.Enumerations;

namespace WebSocketCommunication.EventArguments
{
    public class ConnectionFailedEventArgs
    {
        public WebSocketError Error { get; }

        public ConnectionFailedEventArgs(WebSocketError error)
        {
            Error = error;
        }
    }
}