using System;
using System.Linq;
using System.Text;
using Bogar.BLL.Networking;

namespace Bogar.Tests.BLL.Networking;

public class NetworkMessageTests
{
    [Fact]
    public void Serialize_WithPayload_WritesExpectedFraming()
    {
        var payload = new byte[] { 0x10, 0x20, 0x30 };
        var message = new NetworkMessage(MessageType.ServerGameStart, payload);

        var bytes = message.Serialize();

        Assert.Equal((byte)MessageType.ServerGameStart, bytes[0]);

        var length = BitConverter.ToInt32(bytes, 1);
        Assert.Equal(payload.Length, length);

        var writtenPayload = bytes.Skip(5).ToArray();
        Assert.Equal(payload, writtenPayload);
    }

    [Fact]
    public void CreateText_And_GetText_ShouldRoundTripUtf8()
    {
        const string sample = "SAMPLE TEXT";
        var message = NetworkMessage.CreateText(MessageType.ServerMatchPrepare, sample);

        Assert.Equal(MessageType.ServerMatchPrepare, message.Type);

        var result = message.GetText();
        Assert.Equal(sample, result);
    }

    [Fact]
    public void Serialize_EmptyPayload_WritesZeroLength()
    {
        var message = new NetworkMessage(MessageType.ClientDisconnect);

        var bytes = message.Serialize();

        Assert.Equal((byte)MessageType.ClientDisconnect, bytes[0]);
        var length = BitConverter.ToInt32(bytes, 1);
        Assert.Equal(0, length);
        Assert.Equal(5, bytes.Length);
    }
}
