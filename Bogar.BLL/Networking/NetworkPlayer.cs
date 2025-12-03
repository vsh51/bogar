using Bogar.BLL.Core;
using Bogar.BLL.Player;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Bogar.BLL.Networking;

/// <summary>
/// IPlayer implementation that proxies move requests to a connected network
/// client.
/// </summary>
public sealed class NetworkPlayer : IPlayer, IDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly MessageReader _reader;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly string _nickname;

    public NetworkPlayer(TcpClient client, string nickname)
    {
        this._client = client;
        this._stream = client.GetStream();
        this._reader = new MessageReader(this._stream);
        this._nickname = nickname;
    }

    public string GetMove(List<Move> moves)
    {
        return this.GetMoveAsync(moves).GetAwaiter().GetResult();
    }

    private async Task<string> GetMoveAsync(
        List<Move> moves, CancellationToken cancellationToken = default)
    {
        await this._sendLock.WaitAsync(cancellationToken);
        try
        {
            var payload = SerializeMoves(moves);
            var message = new NetworkMessage(
                MessageType.ServerRequestMove, payload);
            var bytes = message.Serialize();
            await this._stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            await this._stream.FlushAsync(cancellationToken);
        }
        finally
        {
            this._sendLock.Release();
        }

        using var timeoutCts = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);

        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

        var response = await this._reader.ReadMessageAsync(timeoutCts.Token);

        if (response.Type == MessageType.ClientMoveResponse)
        {
            var move = response.GetText();
            if (string.IsNullOrWhiteSpace(move))
                throw new InvalidOperationException(
                    "Client returned empty move");
            return move.Trim();
        }

        if (response.Type == MessageType.ClientDisconnect)
        {
            throw new InvalidOperationException(
                $"Client {this._nickname} disconnected: {response.GetText()}");
        }

        throw new InvalidOperationException(
            $"Unexpected response from {this._nickname}: {response.Type}");
    }

    public async Task SendMatchPrepareAsync(
        string opponentNickname, CancellationToken cancellationToken)
    {
        var message = NetworkMessage.CreateText(
            MessageType.ServerMatchPrepare, opponentNickname);
        await this.SendAsync(message, cancellationToken);
    }

    public async Task SendGameStartAsync(
        string opponentNickname, Color color,
        CancellationToken cancellationToken)
    {
        var payload = $"{opponentNickname}|{color}";
        var message = NetworkMessage.CreateText(
            MessageType.ServerGameStart, payload);
        await this.SendAsync(message, cancellationToken);
    }

    public async Task SendGameEndAsync(
        string result, CancellationToken cancellationToken)
    {
        var message = NetworkMessage.CreateText(
            MessageType.ServerGameEnd, result);
        await this.SendAsync(message, cancellationToken);
    }

    private async Task SendAsync(
        NetworkMessage message, CancellationToken cancellationToken)
    {
        var bytes = message.Serialize();
        await this._sendLock.WaitAsync(cancellationToken);
        try
        {
            await this._stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            await this._stream.FlushAsync(cancellationToken);
        }
        finally
        {
            this._sendLock.Release();
        }
    }

    private static byte[] SerializeMoves(List<Move> moves)
    {
        using var ms = new MemoryStream();
        var countBytes = Encoding.UTF8.GetBytes(moves.Count.ToString() + "\n");
        ms.Write(countBytes, 0, countBytes.Length);

        foreach (var move in moves)
        {
            var moveStr = move.ToString() + "\n";
            var moveBytes = Encoding.UTF8.GetBytes(moveStr);
            ms.Write(moveBytes, 0, moveBytes.Length);
        }

        return ms.ToArray();
    }

    public void Dispose()
    {
        try
        {
            this._sendLock.Dispose();
        }
        catch
        {
        }

        try
        {
            this._stream.Dispose();
        }
        catch
        {
        }

        try
        {
            this._client.Dispose();
        }
        catch
        {
        }
    }
}
