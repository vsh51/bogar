using Bogar.BLL.Core;
using Bogar.BLL.Statistics;
using Bogar.DAL;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using GameInstance = Bogar.BLL.Game.Game;

namespace Bogar.BLL.Networking;

/// <summary>
/// TCP server that manages connections and coordinates matches between clients.
/// </summary>
public sealed class GameServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly ConcurrentDictionary<Guid, ConnectedClient> _clients = new();
    private readonly ConcurrentDictionary<(Guid WhiteId, Guid BlackId), MatchController> _matchControllers = new();
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
    public event Action<MatchResult>? MatchCompleted;

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
            try
            {
                ClientDisconnected?.Invoke(client);
                client.Dispose();
            }
            catch { }
        }
        _clients.Clear();

        foreach (var controller in _matchControllers.Values)
        {
            try { controller.Dispose(); } catch { }
        }
        _matchControllers.Clear();

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

        var matchKey = (whiteId, blackId);
        var controller = new MatchController();
        _matchControllers[matchKey] = controller;

        _ = Task.Run(async () =>
        {
            try
            {
                await RunGameAsync(white, black, controller, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Match error: {ex.Message}");
            }
            finally
            {
                _matchControllers.TryRemove(matchKey, out var existingController);
                existingController?.Dispose();

                lock (_inGameLock)
                {
                    _clientsInGame.Remove(whiteId);
                    _clientsInGame.Remove(blackId);
                }
            }
        });

        return true;
    }

    public bool TryPauseMatch(Guid whiteId, Guid blackId)
    {
        if (_matchControllers.TryGetValue((whiteId, blackId), out var controller))
        {
            return controller.TryPause();
        }

        return false;
    }

    public bool TryResumeMatch(Guid whiteId, Guid blackId)
    {
        if (_matchControllers.TryGetValue((whiteId, blackId), out var controller))
        {
            return controller.TryResume();
        }

        return false;
    }

    public bool IsMatchPaused(Guid whiteId, Guid blackId)
    {
        return _matchControllers.TryGetValue((whiteId, blackId), out var controller) && controller.IsPaused;
    }

    public bool TryKickClient(Guid clientId, out string? error)
    {
        error = null;

        if (!_clients.TryRemove(clientId, out var client))
        {
            error = "Player is no longer connected.";
            return false;
        }

        lock (_inGameLock)
        {
            _clientsInGame.Remove(clientId);
        }

        try
        {
            client.Dispose();
        }
        catch { }

        try
        {
            ClientDisconnected?.Invoke(client);
        }
        catch { }

        LogMessage?.Invoke($"Client kicked: {client.Nickname}");
        return true;
    }

    private async Task AcceptClientsAsync()
    {
        while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(
                    _cancellationTokenSource.Token);
                _ = Task.Run(() => HandleClientAsync(tcpClient,
                    _cancellationTokenSource.Token));
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

    private async Task HandleClientAsync(
        TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var clientId = Guid.NewGuid();
        ConnectedClient? connected = null;

        try
        {
            var stream = tcpClient.GetStream();
            var reader = new MessageReader(stream);

            var registerMessage = await reader.ReadMessageAsync(
                cancellationToken);
            if (registerMessage.Type != MessageType.ClientRegister)
            {
                await SendErrorAsync(stream,
                    "Expected register message", cancellationToken);
                tcpClient.Close();
                return;
            }

            var nickname = registerMessage.GetText().Trim();
            if (string.IsNullOrWhiteSpace(nickname) || nickname.Length > 100)
            {
                await SendErrorAsync(stream,
                    "Invalid nickname", cancellationToken);
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

            var ack = NetworkMessage.CreateText(
                MessageType.ServerRegisterAck, "OK");
            var ackBytes = ack.Serialize();
            await stream.WriteAsync(
                ackBytes, 0, ackBytes.Length, cancellationToken);

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

    private async Task MonitorClientAsync(
        ConnectedClient client, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!IsClientConnected(client.TcpClient))
                    break;
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch
        {
            // TODO: We need to do smth.
        }
    }

    private static bool IsClientConnected(TcpClient tcpClient)
    {
        try
        {
            if (!tcpClient.Connected)
                return false;

            var socket = tcpClient.Client;
            if (socket == null)
                return false;

            return !(socket.Poll(
                0, SelectMode.SelectRead) && socket.Available == 0);
        }
        catch
        {
            return false;
        }
    }

    private async Task RunGameAsync(
        ConnectedClient white, ConnectedClient black,
        MatchController controller,
        CancellationToken cancellationToken)
    {
        var matchStart = DateTimeOffset.UtcNow;
        try
        {
            await NotifyMatchStartingAsync(white, black, cancellationToken);
            await NotifyMatchStartingAsync(black, white, cancellationToken);

            await Task.Delay(500, cancellationToken);

            await white.Player.SendGameStartAsync(
                black.Nickname, Color.White, cancellationToken);
            await black.Player.SendGameStartAsync(
                white.Nickname, Color.Black, cancellationToken);

            GameStarted?.Invoke(white, black);

            var game = new GameInstance(white.Player, black.Player);

            while (!game.IsGameOver() && !cancellationToken.IsCancellationRequested)
            {
                controller.WaitUntilResumed(cancellationToken);

                try
                {
                    var moveColor = game.GetCurrentTurn();
                    await Task.Run(() => game.DoNextMove(), cancellationToken);

                    var lastMove = game.Moves.LastOrDefault();
                    if (lastMove.Piece != Piece.NoPiece)
                    {
                        try
                        {
                            MoveExecuted?.Invoke(
                                white, black, lastMove, moveColor);
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
            var matchFinish = DateTimeOffset.UtcNow;
            var (whiteScore, blackScore) = game.GetScoreBreakdown();
            var moves = game.Moves
                .Where(m => m.Piece != Piece.NoPiece)
                .Select(FormatMove)
                .ToArray();

            MatchCompleted?.Invoke(new MatchResult
            {
                WhiteClientId = white.Id,
                WhiteNickname = white.Nickname,
                BlackClientId = black.Id,
                BlackNickname = black.Nickname,
                Winner = winner,
                Status = MatchStatus.Completed,
                Moves = moves,
                WhiteScore = whiteScore,
                BlackScore = blackScore,
                StartedAt = matchStart,
                FinishedAt = matchFinish,
                IsAutoWin = false
            });
            LogMessage?.Invoke($"Game finished: {result}");
        }
        catch (Exception ex)
        {
            LogMessage?.Invoke($"Run game error: {ex.Message}");
        }
    }

    private static async Task NotifyMatchStartingAsync(
        ConnectedClient target, ConnectedClient opponent,
        CancellationToken cancellationToken)
    {
        await target.Player.SendMatchPrepareAsync(
            opponent.Nickname, cancellationToken);
    }

    private static string FormatMove(Move move)
    {
        var pieceChar = PieceExtensions.TypeOfPiece(move.Piece) switch
        {
            PieceType.Pawn => 'P',
            PieceType.Knight => 'N',
            PieceType.Bishop => 'B',
            PieceType.Rook => 'R',
            PieceType.Queen => 'Q',
            PieceType.King => 'K',
            _ => '?'
        };

        return $"{pieceChar}{SquareExtensions.ToAlgebraic(move.Square).ToUpperInvariant()}";
    }

    private static async Task SendErrorAsync(
        NetworkStream stream, string error, CancellationToken cancellationToken)
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

internal sealed class MatchController : IDisposable
{
    private readonly ManualResetEventSlim _resumeEvent = new(initialState: true);

    public bool IsPaused { get; private set; }

    public void WaitUntilResumed(CancellationToken cancellationToken)
    {
        _resumeEvent.Wait(cancellationToken);
    }

    public bool TryPause()
    {
        if (IsPaused)
            return false;

        IsPaused = true;
        _resumeEvent.Reset();
        return true;
    }

    public bool TryResume()
    {
        if (!IsPaused)
            return false;

        IsPaused = false;
        _resumeEvent.Set();
        return true;
    }

    public void Dispose()
    {
        try { _resumeEvent.Dispose(); } catch { }
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
