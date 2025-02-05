using System.Text.Json;

namespace WebSocketCommunication.Utilities
{
    public static class PacketParser
    {
        public static PacketType GetPacketType(string json)
        {
            string type = JsonDocument.Parse(json).RootElement.GetProperty("Type").GetRawText();
            return JsonSerializer.Deserialize<PacketType>(type);
        }
    }
}
