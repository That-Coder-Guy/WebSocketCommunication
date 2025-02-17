using System.Diagnostics;

namespace WebSocketCommunication
{
    internal class Logger
    {
        public static void Log(string message)
        {
            Debug.Print($"{Thread.CurrentThread.ManagedThreadId} @ {DateTime.Now.TimeOfDay} : {message}");
        }
    }
}
