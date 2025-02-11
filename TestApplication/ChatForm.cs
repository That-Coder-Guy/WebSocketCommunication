using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;

using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketCommunication.WebSockets;

namespace TestApplication
{
    public partial class ChatForm : Form
    {
        public ChatForm()
        {
            StartForm form = new StartForm();
            form.ShowDialog();
            ClientWebSocket webSocket = new ClientWebSocket("ws://localhost:8080/chat");
            webSocket.Connect();

            InitializeComponent();
        }
    }
}
