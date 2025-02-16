using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketCommunication.WebSockets
{
    internal class Logger
    {
        public static void Log(string message)
        {
            Debug.Print($"{Thread.CurrentThread.ManagedThreadId} @ {DateTime.Now.TimeOfDay} : {message}");
        }
    }
}
