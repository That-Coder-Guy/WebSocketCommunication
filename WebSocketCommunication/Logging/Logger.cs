using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketCommunication.Logging
{
    internal class Logger
    {
        public static void Log(string message)
        {
            Debug.Print($"Thread {Thread.CurrentThread.ManagedThreadId} @ {DateTime.Now} : {message}");
        }
    }
}
