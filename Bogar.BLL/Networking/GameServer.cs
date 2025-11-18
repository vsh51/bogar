using Bogar.BLL.Core;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using GameInstance = Bogar.BLL.Game.Game;

namespace Bogar.BLL.Networking;

/// <summary>
/// TCP server that manages connections and coordinates matches between clients.
/// </summary>
public sealed class GameServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly ConcurrentDictionary<Guid, ConnectedClient> _clients = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly object _inGameLock = new();
    private readonly HashSet<Guid> _clientsInGame = new();
    private bool _isRunning;

    public event Action<string>? LogMessage;
    public event Action<ConnectedClient>? ClientConnected;
    public event Action<ConnectedClient>? ClientDisconnected;
    public event Action<ConnectedClient, ConnectedClient>? GameStarted;
    public event Action<ConnectedClient, ConnectedClient, Color?>? GameEnded;
    public event Action<ConnectedClient, ConnectedClient, Move, Color>? MoveExecuted;

    public int Port { get; }

    public GameServer(int port)
    {
        Port = port;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        if (_isRunning)
            return;

        _listener.Start();
        _isRunning = true;
        LogMessage?.Invoke($"Server started on port {Port}");

        _ = Task.Run(AcceptClientsAsync, _cancellationTokenSource.Token);
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _cancellationTokenSource.Cancel();

        try { _listener.Stop(); } catch { }

        foreach (var client in _clients.Values)
        {
            try { client.Dispose(); } catch { }
        }
        _clients.Clear();

        lock (_inGameLock)
        {
            _clientsInGame.Clear();
        }

        LogMessage?.Invoke("Server stopped");
    }

    public IReadOnlyList<ConnectedClient> GetConnectedClients()
    {
        return _clients.Values
            .OrderBy(c => c.Nickname, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool IsClientInGame(Guid clientId)
    {
        lock (_inGameLock)
        {
            return _clientsInGame.Contains(clientId);
        }
    }

    public bool TryStartMatch(Guid whiteId, Guid blackId, out string? error)
    {
        error = null;
        if (whiteId == blackId)
        {
            error = "Select two different players.";
            return false;
        }

        if (!_clients.TryGetValue(whiteId, out var white))
        {
            error = "White player disconnected.";
            return false;
        }

        if (!_clients.TryGetValue(blackId, out var black))
        {
            error = "Black player disconnected.";
            return false;
        }

        lock (_inGameLock)
        {
            if (_clientsInGame.Contains(whiteId) || _clientsInGame.Contains(blackId))
            {
                error = "One of the players is already in a match.";
                return false;
            }

            _clientsInGame.Add(whiteId);
            _clientsInGame.Add(blackId);
        }

        LogMessage?.Invoke($"Starting match {white.Nickname} vs {black.Nickname}");

        _ = Task.Run(async () =>
        {
            try
            {
                await RunGameAsync(white, black, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Match error: {ex.Message}");
            }
            finally
            {
                lock (_inGameLock)
                {
                    _clientsInGame.Remove(whiteId);
                    _clientsInGame.Remove(blackId);
                }
            }
        });

        return true;
    }

    private async Task AcceptClientsAsync()
    {
        while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
                _ = Task.Run(() => HandleClientAsync(tcpClient, _cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    LogMessage?.Invoke($"Accept error: {ex.Message}");
                }
            }
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var clientId = Guid.NewGuid();
        ConnectedClient? connected = null;

        try
        {
            var stream = tcpClient.GetStream();
            var reader = new MessageReader(stream);

            var registerMessage = await reader.ReadMessageAsync(cancellationToken);
            if (registerMessage.Type != MessageType.ClientRegister)
            {
                await SendErrorAsync(stream, "Expected register message", cancellationToken);
                tcpClient.Close();
                return;
            }

            var nickname = registerMessage.GetText().Trim();
            if (string.IsNullOrWhiteSpace(nickname) || nickname.Length > 100)
            {
                await SendErrorAsync(stream, "Invalid nickname", cancellationToken);
                tcpClient.Close();
                return;
            }

            connected = new ConnectedClient
            {
                Id = clientId,
                Nickname = nickname,
                TcpClient = tcpClient,
                Player = new NetworkPlayer(tcpClient, nickname)
            };

            _clients[clientId] = connected;

            var ack = NetworkMessage.CreateText(MessageType.ServerRegisterAck, "OK");
            var ackBytes = ack.Serialize();
            await stream.WriteAsync(ackBytes, 0, ackBytes.Length, cancellationToken);

            LogMessage?.Invoke($"Client connected: {nickname}");
            ClientConnected?.Invoke(connected);

            await MonitorClientAsync(connected, cancellationToken);
        }
        catch (Exception ex)
        {
            LogMessage?.Invoke($"Client error: {ex.Message}");
        }
        finally
        {
            if (connected != null)
            {
                _clients.TryRemove(clientId, out _);
                ClientDisconnected?.Invoke(connected);
                lock (_inGameLock)
                {
                    _clientsInGame.Remove(clientId);
                }
                connected.Dispose();
            }
            else
            {
                tcpClient.Dispose();
            }
        }
    }

    private async Task MonitorClientAsync(ConnectedClient client, CancellationToken cancellationToken)
    {
        try
        {
            while (client.TcpClient.Connected && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch
        {
            // ignored
        }
    }

    private async Task RunGameAsync(ConnectedClient white, ConnectedClient black, CancellationToken cancellationToken)
    {
        try
        {
            await NotifyMatchStartingAsync(white, black, cancellationToken);
            await NotifyMatchStartingAsync(black, white, cancellationToken);

            await Task.Delay(500, cancellationToken);

            await white.Player.SendGameStartAsync(black.Nickname, Color.White, cancellationToken);
            await black.Player.SendGameStartAsync(white.Nickname, Color.Black, cancellationToken);

            GameStarted?.Invoke(white, black);

            var game = new GameInstance(white.Player, black.Player);

            while (!game.IsGameOver() && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var moveColor = game.GetCurrentTurn();
                    await Task.Run(() => game.DoNextMove(), cancellationToken);

                    var lastMove = game.Moves.LastOrDefault();
                    if (lastMove.Piece != Piece.NoPiece)
                    {
                        try
                        {
                            MoveExecuted?.Invoke(white, black, lastMove, moveColor);
                        }
                        catch { }
                    }
                    await Task.Delay(50, cancellationToken);
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"Game loop error: {ex.Message}");
                    break;
                }
            }

            Color? winner = null;
            if (game.IsGameOver())
            {
                var score = game.GetScore();
                winner = score > 0 ? Color.White : score < 0 ? Color.Black : null;
            }

            string result = winner switch
            {
                Color.White => $"{white.Nickname} wins!",
                Color.Black => $"{black.Nickname} wins!",
                _ => "Draw"
            };

            await white.Player.SendGameEndAsync(result, cancellationToken);
            await black.Player.SendGameEndAsync(result, cancellationToken);

            GameEnded?.Invoke(white, black, winner);
            LogMessage?.Invoke($"Game finished: {result}");
        }
        catch (Exception ex)
        {
            LogMessage?.Invoke($"Run game error: {ex.Message}");
        }
    }

    private static async Task NotifyMatchStartingAsync(ConnectedClient target, ConnectedClient opponent, CancellationToken cancellationToken)
    {
        await target.Player.SendMatchPrepareAsync(opponent.Nickname, cancellationToken);
    }

    private static async Task SendErrorAsync(NetworkStream stream, string error, CancellationToken cancellationToken)
    {
        var message = NetworkMessage.CreateText(MessageType.ServerError, error);
        var bytes = message.Serialize();
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource.Dispose();
    }
}

public sealed class ConnectedClient : IDisposable
{
    public Guid Id { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public TcpClient TcpClient { get; set; } = null!;
    public NetworkPlayer Player { get; set; } = null!;

    public void Dispose()
    {
        try { Player?.Dispose(); } catch { }
        try { TcpClient?.Close(); } catch { }
    }
}
