using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bogar.BLL.Networking;
using Bogar.Tests.TestHelpers;

namespace Bogar.Tests.BLL.Networking;

public sealed class MessageReaderTests
{
    [Fact]
    public async Task ReadMessageAsync_WithValidFrame_ReturnsMessage()
    {
        await using var pair = await ConnectedStreamPair.CreateAsync();
        var reader = new MessageReader(pair.ReaderStream);

        var payload = Encoding.UTF8.GetBytes("payload");
        var message = new NetworkMessage(MessageType.ServerGameEnd, payload);
        var bytes = message.Serialize();

        await pair.WriterStream.WriteAsync(bytes.AsMemory(0, 3));
        await pair.WriterStream.WriteAsync(bytes.AsMemory(3));
        await pair.WriterStream.FlushAsync();

        var result = await reader.ReadMessageAsync(CancellationToken.None);

        Assert.Equal(MessageType.ServerGameEnd, result.Type);
        Assert.Equal(payload, result.Payload);
    }

    [Fact]
    public async Task ReadMessageAsync_LengthTooLarge_Throws()
    {
        await using var pair = await ConnectedStreamPair.CreateAsync();
        var reader = new MessageReader(pair.ReaderStream);

        var header = new byte[5];
        header[0] = (byte)MessageType.ServerError;
        BitConverter.GetBytes((5 * 1024) + 1).CopyTo(header, 1);

        await pair.WriterStream.WriteAsync(header, 0, header.Length);
        await pair.WriterStream.FlushAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.ReadMessageAsync(CancellationToken.None));
        Assert.Contains("Invalid payload length", ex.Message);
    }

    [Fact]
    public async Task ReadMessageAsync_StreamClosedBeforePayload_Throws()
    {
        await using var pair = await ConnectedStreamPair.CreateAsync();
        var reader = new MessageReader(pair.ReaderStream);

        var header = new byte[5];
        header[0] = (byte)MessageType.ServerRequestMove;
        BitConverter.GetBytes(10).CopyTo(header, 1);

        await pair.WriterStream.WriteAsync(header, 0, header.Length);
        await pair.WriterStream.FlushAsync();
        pair.CloseWriter();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.ReadMessageAsync(CancellationToken.None));
        Assert.Contains("Stream closed unexpectedly", ex.Message);
    }

}
