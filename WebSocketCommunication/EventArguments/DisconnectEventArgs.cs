using WebSocketCommunication.Enumerations;

namespace WebSocketCommunication.EventArguments
{
    public class DisconnectEventArgs
    {
        public WebSocketClosureReason Reason { get; }

        public DisconnectEventArgs(WebSocketClosureReason reason)
        {
            Reason = reason;
        }
    }
}