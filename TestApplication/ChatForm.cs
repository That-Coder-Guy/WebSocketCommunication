using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketCommunication.EventArguments;
using WebSocketCommunication.WebSockets;

namespace TestApplication
{
    public partial class ChatForm : Form
    {
        private ClientWebSocket _webSocket;

        public ChatForm()
        {
            StartForm form = new StartForm();
            form.ShowDialog();
            InitializeComponent();

            _webSocket = new ClientWebSocket("ws://localhost:8080/chat/");
            _webSocket.Connected += OnConnected;
            _webSocket.ConnectionFailed += OnConnectionFailed;
            _webSocket.BeginConnect();
        }

        private void OnConnected(object? sender, EventArgs e)
        {
            Debug.Print("Connected");
        }

        private void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
        {
            Debug.Print("Connection failed");
        }

        private void EndClicked(object sender, EventArgs e)
        {
            _webSocket.Disconnect();
            Close();
        }
    }
}
