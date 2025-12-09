using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bogar.BLL.Core;
using Bogar.BLL.Networking;
using Bogar.Tests.TestHelpers;

namespace Bogar.Tests.BLL.Networking;

public sealed class NetworkPlayerTests
{
    [Fact]
    public async Task GetMove_ReturnsResponseFromClient()
    {
        await using var pair = await ConnectedStreamPair.CreateAsync();
        using var player = new NetworkPlayer(pair.ReaderClient, "Tester");

        var moves = new List<Move> { new Move(Piece.WhitePawn, Square.A2) };

        var getMoveTask = Task.Run(() => player.GetMove(moves));

        var reader = new MessageReader(pair.WriterStream);
        var request = await reader.ReadMessageAsync(CancellationToken.None);

        Assert.Equal(MessageType.ServerRequestMove, request.Type);
        var payloadText = Encoding.UTF8.GetString(request.Payload);
        var lines = payloadText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("1", lines[0]);
        Assert.Equal(moves[0].ToString(), lines[1]);

        var response = NetworkMessage.CreateText(MessageType.ClientMoveResponse, "Pe4");
        var bytes = response.Serialize();
        await pair.WriterStream.WriteAsync(bytes, 0, bytes.Length);
        await pair.WriterStream.FlushAsync();

        var result = await getMoveTask;
        Assert.Equal("Pe4", result);
    }

    [Fact]
    public async Task GetMove_ClientDisconnect_Throws()
    {
        await using var pair = await ConnectedStreamPair.CreateAsync();
        using var player = new NetworkPlayer(pair.ReaderClient, "Tester");

        var getMoveTask = Task.Run(() => player.GetMove(new List<Move>()));

        var reader = new MessageReader(pair.WriterStream);
        await reader.ReadMessageAsync(CancellationToken.None);

        var disconnect = NetworkMessage.CreateText(MessageType.ClientDisconnect, "Bot execution error");
        await pair.WriterStream.WriteAsync(disconnect.Serialize());
        await pair.WriterStream.FlushAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await getMoveTask);
        Assert.Contains("Bot execution error", ex.Message);
    }

    [Fact]
    public async Task GetMove_ClientReturnsEmptyMove_Throws()
    {
        await using var pair = await ConnectedStreamPair.CreateAsync();
        using var player = new NetworkPlayer(pair.ReaderClient, "Tester");

        var getMoveTask = Task.Run(() => player.GetMove(new List<Move>()));
        var reader = new MessageReader(pair.WriterStream);
        await reader.ReadMessageAsync(CancellationToken.None);

        var emptyResponse = NetworkMessage.CreateText(MessageType.ClientMoveResponse, "   ");
        await pair.WriterStream.WriteAsync(emptyResponse.Serialize());
        await pair.WriterStream.FlushAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await getMoveTask);
        Assert.Contains("empty move", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
