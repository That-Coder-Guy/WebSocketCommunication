namespace WebSocketCommunication.Utilities
{
    public class SimpleStringPacket : Packet
    {
        public PacketType Type => PacketType.SimpleString;

        public string Content { get; }

        public SimpleStringPacket(string content)
        {
            Content = content;
        }
    }
}
