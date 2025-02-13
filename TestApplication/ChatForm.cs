using System.Diagnostics;
using System.Text;
using WebSocketCommunication.EventArguments;
using WebSocketCommunication.WebSockets;

namespace TestApplication
{
    public partial class ChatForm : Form
    {
        private ClientWebSocket _webSocket;

        private string _name;

        public ChatForm()
        {
            StartForm form = new StartForm();
            form.ShowDialog();
            _name = form.EnteredName;
            InitializeComponent();

            _webSocket = new ClientWebSocket("ws://localhost:8080/chat/");
            _webSocket.Connected += OnConnected;
            _webSocket.ConnectionFailed += OnConnectionFailed;
            _webSocket.MessageReceived += OnMessageReceived;
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

        private void OnMessageReceived(object? sender, MessageEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Data.ToArray());
            Debug.Print(message);
            Invoke(() =>
            {
                uxChatMessages.Items.Add(message);
            });
        }

        private void OnEndClicked(object sender, EventArgs e)
        {
            _webSocket.Disconnect();
            Close();
        }

        private void OnSendClick(object sender, EventArgs e)
        {
            if (uxMessageInputBox.Text != "")
            {
                Send();
            }
        }

        private void OnMessageInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && uxMessageInputBox.Text != "")
            {
                Send();
            }
        }

        private void Send()
        {
            byte[] data = Encoding.UTF8.GetBytes($"{_name} : {uxMessageInputBox.Text}");
            _webSocket.Send(data);
            uxMessageInputBox.Text = "";
        }

        private void OnClosed(object sender, FormClosedEventArgs e)
        {
            _webSocket.Disconnect();
        }
    }
}
