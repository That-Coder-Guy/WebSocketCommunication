using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketCommunication.Logging
{
    public class Logger
    {
        public static void Log(string message)
        {
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} @ {DateTime.Now} : {message}");
        }
    }
}
