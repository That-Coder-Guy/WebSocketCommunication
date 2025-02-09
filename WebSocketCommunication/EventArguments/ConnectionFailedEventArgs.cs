using WebSocketCommunication.Enumerations;

namespace WebSocketCommunication.EventArguments
{
    public class ConnectionFailedEventArgs
    {
        public ConnectionFailedReason Reason { get; }

        public ConnectionFailedEventArgs(ConnectionFailedReason reason)
        {
            Reason = reason;
        }
    }
}