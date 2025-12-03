using Bogar.BLL.Core;
using Bogar.BLL.Statistics;
using Bogar.DAL;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using Serilog;
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
    private static readonly Serilog.ILogger Logger = Log.ForContext<GameServer>();

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
        this.Port = port;
        this._listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        if (this._isRunning)
        {
            return;
        }

        this._listener.Start();
        this._isRunning = true;
        this.LogMessage?.Invoke($"Server started on port {this.Port}");
        Logger.Information("Server started on port {Port}", this.Port);

        _ = Task.Run(this.AcceptClientsAsync, this._cancellationTokenSource.Token);
    }

    public void Stop()
    {
        if (!this._isRunning)
        {
            return;
        }

        this._isRunning = false;
        this._cancellationTokenSource.Cancel();

        try
        {
            this._listener.Stop();
        }
        catch
        {
        }

        foreach (var client in this._clients.Values)
        {
            try
            {
                this.ClientDisconnected?.Invoke(client);
                client.Dispose();
            }
            catch
            {
            }
        }
        this._clients.Clear();

        foreach (var controller in this._matchControllers.Values)
        {
            try
            {
                controller.Dispose();
            }
            catch
            {
            }
        }
        this._matchControllers.Clear();

        lock (this._inGameLock)
        {
            this._clientsInGame.Clear();
        }

        this.LogMessage?.Invoke("Server stopped");
        Logger.Information("Server stopped");
    }

    public IReadOnlyList<ConnectedClient> GetConnectedClients()
    {
        return this._clients.Values
            .OrderBy(c => c.Nickname, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool IsClientInGame(Guid clientId)
    {
        lock (this._inGameLock)
        {
            return this._clientsInGame.Contains(clientId);
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

        if (!this._clients.TryGetValue(whiteId, out var white))
        {
            error = "White player disconnected.";
            return false;
        }

        if (!this._clients.TryGetValue(blackId, out var black))
        {
            error = "Black player disconnected.";
            return false;
        }

        lock (this._inGameLock)
        {
            if (this._clientsInGame.Contains(whiteId) || this._clientsInGame.Contains(blackId))
            {
                error = "One of the players is already in a match.";
                return false;
            }

            this._clientsInGame.Add(whiteId);
            this._clientsInGame.Add(blackId);
        }

        this.LogMessage?.Invoke($"Starting match {white.Nickname} vs {black.Nickname}");
        Logger.Information("Starting match {White} vs {Black}", white.Nickname, black.Nickname);

        var matchKey = (whiteId, blackId);
        var controller = new MatchController();
        this._matchControllers[matchKey] = controller;

        var matchToken = controller.MatchCancellation.Token;
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this._cancellationTokenSource.Token, matchToken);

        _ = Task.Run(async () =>
        {
            try
            {
                await this.RunGameAsync(white, black, controller, linkedTokenSource.Token);
            }
            catch (Exception ex)
            {
                this.LogMessage?.Invoke($"Match error: {ex.Message}");
                Logger.Error(ex, "Match error while running {White} vs {Black}", white.Nickname, black.Nickname);
            }
            finally
            {
                linkedTokenSource.Dispose();
                this._matchControllers.TryRemove(matchKey, out var existingController);
                existingController?.Dispose();

                lock (this._inGameLock)
                {
                    this._clientsInGame.Remove(whiteId);
                    this._clientsInGame.Remove(blackId);
                }
            }
        });

        return true;
    }

    public bool TryPauseMatch(Guid whiteId, Guid blackId)
    {
        if (this._matchControllers.TryGetValue((whiteId, blackId), out var controller))
        {
            return controller.TryPause();
        }

        return false;
    }

    public bool TryResumeMatch(Guid whiteId, Guid blackId)
    {
        if (this._matchControllers.TryGetValue((whiteId, blackId), out var controller))
        {
            return controller.TryResume();
        }

        return false;
    }

    public bool StopMatch(Guid whiteId, Guid blackId)
    {
        if (this._matchControllers.TryGetValue((whiteId, blackId), out var controller))
        {
            controller.MatchCancellation.Cancel();
            return true;
        }
        return false;
    }

    public bool IsMatchPaused(Guid whiteId, Guid blackId)
    {
        return this._matchControllers.TryGetValue((whiteId, blackId), out var controller) && controller.IsPaused;
    }

    public bool TryKickClient(Guid clientId, out string? error)
    {
        error = null;

        if (!this._clients.TryRemove(clientId, out var client))
        {
            error = "Player is no longer connected.";
            return false;
        }

        lock (this._inGameLock)
        {
            this._clientsInGame.Remove(clientId);
        }

        try
        {
            client.Dispose();
        }
        catch
        {
        }

        try
        {
            this.ClientDisconnected?.Invoke(client);
        }
        catch
        {
        }

        this.LogMessage?.Invoke($"Client kicked: {client.Nickname}");
        Logger.Information("Client {Nickname} was kicked", client.Nickname);
        return true;
    }

    private async Task AcceptClientsAsync()
    {
        while (this._isRunning && !this._cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await this._listener.AcceptTcpClientAsync(
                    this._cancellationTokenSource.Token);
                _ = Task.Run(() => this.HandleClientAsync(tcpClient,
                    this._cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (this._isRunning)
                {
                    this.LogMessage?.Invoke($"Accept error: {ex.Message}");
                    Logger.Error(ex, "Failed to accept incoming client");
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

            this._clients[clientId] = connected;

            var ack = NetworkMessage.CreateText(
                MessageType.ServerRegisterAck, "OK");
            var ackBytes = ack.Serialize();
            await stream.WriteAsync(
                ackBytes, 0, ackBytes.Length, cancellationToken);

            this.LogMessage?.Invoke($"Client connected: {nickname}");
            Logger.Information("Client connected: {Nickname}", nickname);
            this.ClientConnected?.Invoke(connected);

            await this.MonitorClientAsync(connected, cancellationToken);
        }
        catch (Exception ex)
        {
            this.LogMessage?.Invoke($"Client error: {ex.Message}");
            Logger.Error(ex, "Client error for ID {ClientId}", clientId);
        }
        finally
        {
            if (connected != null)
            {
                this._clients.TryRemove(clientId, out _);
                this.ClientDisconnected?.Invoke(connected);
                lock (this._inGameLock)
                {
                    this._clientsInGame.Remove(clientId);
                }
                connected.Dispose();
                Logger.Information("Client disconnected: {Nickname}", connected.Nickname);
            }
            else
            {
                tcpClient.Dispose();
                Logger.Information("Unregistered client {ClientId} disconnected before registration", clientId);
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
                {
                    break;
                }
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Client monitor loop ended for {Nickname}", client.Nickname);
        }
    }

    private static bool IsClientConnected(TcpClient tcpClient)
    {
        try
        {
            if (!tcpClient.Connected)
            {
                return false;
            }

            var socket = tcpClient.Client;
            if (socket == null)
            {
                return false;
            }

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

            this.GameStarted?.Invoke(white, black);

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
                            this.MoveExecuted?.Invoke(
                                white, black, lastMove, moveColor);
                        }
                        catch
                        {
                        }
                    }
                    await Task.Delay(50, cancellationToken);
                }
                catch (Exception ex)
                {
                    this.LogMessage?.Invoke($"Game loop error: {ex.Message}");
                    Logger.Error(ex, "Game loop error for {White} vs {Black}", white.Nickname, black.Nickname);
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

            this.GameEnded?.Invoke(white, black, winner);
            var matchFinish = DateTimeOffset.UtcNow;
            var (whiteScore, blackScore) = game.GetScoreBreakdown();
            var moves = game.Moves
                .Where(m => m.Piece != Piece.NoPiece)
                .Select(FormatMove)
                .ToArray();

            this.MatchCompleted?.Invoke(new MatchResult
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
            this.LogMessage?.Invoke($"Game finished: {result}");
            Logger.Information("Game finished: {Result}", result);
        }
        catch (Exception ex)
        {
            this.LogMessage?.Invoke($"Run game error: {ex.Message}");
            Logger.Error(ex, "Run game error for {White} vs {Black}", white.Nickname, black.Nickname);
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
        this.Stop();
        this._cancellationTokenSource.Dispose();
    }
}

internal sealed class MatchController : IDisposable
{
    private readonly ManualResetEventSlim _resumeEvent = new(initialState: true);
    public CancellationTokenSource MatchCancellation { get; } = new();

    public bool IsPaused { get; private set; }

    public void WaitUntilResumed(CancellationToken cancellationToken)
    {
        this._resumeEvent.Wait(cancellationToken);
    }

    public bool TryPause()
    {
        if (this.IsPaused)
        {
            return false;
        }

        this.IsPaused = true;
        this._resumeEvent.Reset();
        return true;
    }

    public bool TryResume()
    {
        if (!this.IsPaused)
        {
            return false;
        }

        this.IsPaused = false;
        this._resumeEvent.Set();
        return true;
    }

    public void Dispose()
    {
        try
        {
            this._resumeEvent.Dispose();
        }
        catch
        {
        }

        try
        {
            this.MatchCancellation.Dispose();
        }
        catch
        {
        }
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
        try
        {
            this.Player?.Dispose();
        }
        catch
        {
        }

        try
        {
            this.TcpClient?.Close();
        }
        catch
        {
        }
    }
}
