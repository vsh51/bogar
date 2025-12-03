using Bogar.BLL.Core;
using Bogar.BLL.Player;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Serilog;

namespace Bogar.BLL.Networking;

/// <summary>
/// Handles client side networking and runs the local bot executable.
/// </summary>
public sealed class GameClient : IDisposable
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private MessageReader? _reader;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private Player.Player? _localBot;
    private static readonly Serilog.ILogger Logger = Log.ForContext<GameClient>();

    public event Action<string>? LogMessage;
    public event Action<string>? MatchPreparing;
    public event Action<string, Color>? GameStarted;
    public event Action<string>? GameEnded;
    public event Action<string>? ErrorReceived;
    public event Action? Disconnected;

    public string? Nickname { get; private set; }
    public bool IsConnected => this._tcpClient?.Connected ?? false;

    public async Task<bool> ConnectAsync(
        string host, int port,
        string nickname, string botPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(botPath))
        {
            const string message = "Bot executable not found.";
            this.LogMessage?.Invoke(message);
            Logger.Warning("Bot executable missing at {BotPath}", botPath);
            return false;
        }

        try
        {
            Logger.Information("Connecting to {Host}:{Port} as {Nickname}", host, port, nickname);
            this._localBot = new Player.Player(botPath);
            this._tcpClient = new TcpClient();
            await this._tcpClient.ConnectAsync(host, port, cancellationToken);
            this._stream = this._tcpClient.GetStream();
            this._reader = new MessageReader(this._stream);
            this.Nickname = nickname;

            var register = NetworkMessage.CreateText(
                MessageType.ClientRegister, nickname
            );
            await this.SendAsync(register, cancellationToken);

            var ack = await this._reader.ReadMessageAsync(cancellationToken);
            if (ack.Type != MessageType.ServerRegisterAck)
            {
                this.ErrorReceived?.Invoke("Server rejected registration.");
                Logger.Warning("Registration rejected by server at {Host}:{Port}", host, port);
                return false;
            }

            this.LogMessage?.Invoke("Connected to server.");
            Logger.Information("Connected to {Host}:{Port} as {Nickname}", host, port, nickname);
            _ = Task.Run(() => this.ListenAsync(
                cancellationToken), cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            this.ErrorReceived?.Invoke($"Connection error: {ex.Message}");
            Logger.Error(ex, "Failed to connect to {Host}:{Port} as {Nickname}", host, port, nickname);
            return false;
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (this.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                var message = await this._reader!.ReadMessageAsync(
                    cancellationToken
                );

                switch (message.Type)
                {
                    case MessageType.ServerMatchPrepare:
                        var opponentPreparing = message.GetText();
                        Logger.Information("Match preparing against {Opponent}", opponentPreparing);
                        this.MatchPreparing?.Invoke(opponentPreparing);
                        break;
                    case MessageType.ServerGameStart:
                        var parts = message.GetText().Split('|');
                        var opponent = parts.ElementAtOrDefault(0) ?? "Unknown";
                        var color = parts.Length > 1
                            && Enum.TryParse<Color>(parts[1], out var parsedColor)
                            ? parsedColor
                            : Color.White;
                        Logger.Information("Game started vs {Opponent} as {Color}", opponent, color);
                        this.GameStarted?.Invoke(opponent, color);
                        break;
                    case MessageType.ServerRequestMove:
                        Logger.Debug("Server requested move from bot {Nickname}", this.Nickname);
                        await this.HandleMoveRequestAsync(
                            message.Payload, cancellationToken);
                        break;
                    case MessageType.ServerGameEnd:
                        var result = message.GetText();
                        Logger.Information("Game ended: {Result}", result);
                        this.GameEnded?.Invoke(result);
                        break;
                    case MessageType.ServerError:
                        var error = message.GetText();
                        Logger.Warning("Server error received: {Message}", error);
                        this.ErrorReceived?.Invoke(error);
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
            this.ErrorReceived?.Invoke("Server shutdown — connection closed.");
            Logger.Warning("Server shutdown detected");
        }
        catch (SocketException)
        {
            this.ErrorReceived?.Invoke("Connection lost.");
            Logger.Warning("Socket connection lost");
        }
        catch (Exception ex)
        {
            this.ErrorReceived?.Invoke($"Network error: {ex.Message}");
            Logger.Error(ex, "Unexpected network error");
        }
        finally
        {
            this.Disconnect();
            this.Disconnected?.Invoke();
            Logger.Information("Disconnected from server");
        }
    }

    private async Task HandleMoveRequestAsync(
        byte[] payload, CancellationToken cancellationToken)
    {
        if (this._localBot == null)
        {
            await this.SendErrorAsync("Bot not initialized", cancellationToken);
            Logger.Warning("Move requested before bot initialization");
            return;
        }

        var payloadText = Encoding.UTF8.GetString(payload);
        var lines = payloadText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var moves = new List<Move>();

        if (lines.Length > 0 && int.TryParse(lines[0], out var moveCount))
        {
            var moveStrings = lines.Skip(1).Take(moveCount);
            int index = 0;
            foreach (var move in moveStrings)
            {
                if (
                    TryParseMove(
                        move.Trim(),
                        index % 2 == 0 ? Color.White : Color.Black,
                        out var parsed)
                )
                {
                    moves.Add(parsed);
                }
                index++;
            }
        }

        string resultMove;
        try
        {
            resultMove = this._localBot.GetMove(moves);
        }
        catch (Exception ex)
        {
            await this.SendErrorAsync(
                $"Bot execution error: {ex.Message}", cancellationToken);
            Logger.Error(ex, "Bot execution failed when generating move");
            return;
        }

        if (string.IsNullOrWhiteSpace(resultMove))
        {
            await this.SendErrorAsync(
                "Bot returned empty move", cancellationToken);
            Logger.Warning("Bot returned an empty move");
            return;
        }

        var response = NetworkMessage.CreateText(
            MessageType.ClientMoveResponse, resultMove.Trim());
        await this.SendAsync(response, cancellationToken);
        Logger.Debug("Sent move response {Move}", resultMove.Trim());
    }

    private static bool TryParseMove(
        string moveString, Color color, out Move move)
    {
        move = default;
        if (string.IsNullOrWhiteSpace(moveString) || moveString.Length < 3)
        {
            return false;
        }

        char pieceChar = moveString[0];
        string squareString = moveString.Substring(1).ToUpperInvariant();

        if (!Enum.TryParse<Square>(squareString, out var square))
        {
            return false;
        }

        Piece piece = color switch
        {
            Color.White => pieceChar switch
            {
                'P' => Piece.WhitePawn,
                'N' => Piece.WhiteKnight,
                'B' => Piece.WhiteBishop,
                'R' => Piece.WhiteRook,
                'Q' => Piece.WhiteQueen,
                'K' => Piece.WhiteKing,
                _ => Piece.NoPiece
            },
            _ => pieceChar switch
            {
                'P' => Piece.BlackPawn,
                'N' => Piece.BlackKnight,
                'B' => Piece.BlackBishop,
                'R' => Piece.BlackRook,
                'Q' => Piece.BlackQueen,
                'K' => Piece.BlackKing,
                _ => Piece.NoPiece
            }
        };

        if (piece == Piece.NoPiece)
        {
            return false;
        }

        move = new Move(piece, square);
        return true;
    }

    private async Task SendErrorAsync(string error, CancellationToken cancellationToken)
    {
        this.ErrorReceived?.Invoke(error);
        var message = NetworkMessage.CreateText(MessageType.ClientDisconnect, error);
        await this.SendAsync(message, cancellationToken);
        Logger.Error("Client error reported to server: {Error}", error);
    }

    private async Task SendAsync(NetworkMessage message, CancellationToken cancellationToken)
    {
        if (this._stream == null)
        {
            return;
        }

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

    public void Disconnect()
    {
        try
        {
            this._tcpClient?.Close();
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Error while closing TCP client");
        }
    }

    public void Dispose()
    {
        try
        {
            this._sendLock.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Error disposing sendLock");
        }

        try
        {
            this._stream?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Error disposing network stream");
        }

        try
        {
            this._tcpClient?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Error disposing TCP client");
        }
    }
}
