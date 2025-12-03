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
        _client = client;
        _stream = client.GetStream();
        _reader = new MessageReader(_stream);
        _nickname = nickname;
    }

    public string GetMove(List<Move> moves)
    {
        return GetMoveAsync(moves).GetAwaiter().GetResult();
    }

    private async Task<string> GetMoveAsync(
        List<Move> moves, CancellationToken cancellationToken = default)
    {
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            var payload = SerializeMoves(moves);
            var message = new NetworkMessage(
                MessageType.ServerRequestMove, payload);
            var bytes = message.Serialize();
            await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }

        using var timeoutCts = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);

        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

        var response = await _reader.ReadMessageAsync(timeoutCts.Token);

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
                $"Client {_nickname} disconnected: {response.GetText()}");
        }

        throw new InvalidOperationException(
            $"Unexpected response from {_nickname}: {response.Type}");
    }

    public async Task SendMatchPrepareAsync(
        string opponentNickname, CancellationToken cancellationToken)
    {
        var message = NetworkMessage.CreateText(
            MessageType.ServerMatchPrepare, opponentNickname);
        await SendAsync(message, cancellationToken);
    }

    public async Task SendGameStartAsync(
        string opponentNickname, Color color,
        CancellationToken cancellationToken)
    {
        var payload = $"{opponentNickname}|{color}";
        var message = NetworkMessage.CreateText(
            MessageType.ServerGameStart, payload);
        await SendAsync(message, cancellationToken);
    }

    public async Task SendGameEndAsync(
        string result, CancellationToken cancellationToken)
    {
        var message = NetworkMessage.CreateText(
            MessageType.ServerGameEnd, result);
        await SendAsync(message, cancellationToken);
    }

    private async Task SendAsync(
        NetworkMessage message, CancellationToken cancellationToken)
    {
        var bytes = message.Serialize();
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }
        finally
        {
            _sendLock.Release();
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
            _sendLock.Dispose();
        }
        catch
        {
        }

        try
        {
            _stream.Dispose();
        }
        catch
        {
        }

        try
        {
            _client.Dispose();
        }
        catch
        {
        }
    }
}
