using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketCommunication.Utilities
{
    public class MessageEventArgs : EventArgs
    {
        public MemoryStream Data;

        public MessageEventArgs(MemoryStream data)
        {
            Data = data;
        }
    }
}
