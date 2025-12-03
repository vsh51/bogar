using System.Text;

namespace Bogar.BLL.Networking;

/// <summary>
/// Represents a framed message consisting of a type byte and payload length.
/// </summary>
public sealed class NetworkMessage
{
    public MessageType Type { get; }
    public byte[] Payload { get; }

    public NetworkMessage(MessageType type, byte[]? payload = null)
    {
        this.Type = type;
        this.Payload = payload ?? Array.Empty<byte>();
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        ms.WriteByte((byte)this.Type);
        var lengthBytes = BitConverter.GetBytes(this.Payload.Length);
        ms.Write(lengthBytes, 0, lengthBytes.Length);
        if (this.Payload.Length > 0)
        {
            ms.Write(this.Payload, 0, this.Payload.Length);
        }
        return ms.ToArray();
    }

    public static NetworkMessage CreateText(MessageType type, string text)
    {
        return new NetworkMessage(type, Encoding.UTF8.GetBytes(text));
    }

    public string GetText()
    {
        return Encoding.UTF8.GetString(this.Payload);
    }
}
