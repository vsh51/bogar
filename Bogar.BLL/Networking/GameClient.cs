using Bogar.BLL.Core;
using Bogar.BLL.Player;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

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

    public event Action<string>? LogMessage;
    public event Action<string>? MatchPreparing;
    public event Action<string, Color>? GameStarted;
    public event Action<string>? GameEnded;
    public event Action<string>? ErrorReceived;

    public string? Nickname { get; private set; }
    public bool IsConnected => _tcpClient?.Connected ?? false;

    public async Task<bool> ConnectAsync(string host, int port, string nickname, string botPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(botPath))
        {
            LogMessage?.Invoke("Bot executable not found.");
            return false;
        }

        try
        {
            _localBot = new Player.Player(botPath);
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port, cancellationToken);
            _stream = _tcpClient.GetStream();
            _reader = new MessageReader(_stream);
            Nickname = nickname;

            var register = NetworkMessage.CreateText(MessageType.ClientRegister, nickname);
            await SendAsync(register, cancellationToken);

            var ack = await _reader.ReadMessageAsync(cancellationToken);
            if (ack.Type != MessageType.ServerRegisterAck)
            {
                ErrorReceived?.Invoke("Server rejected registration.");
                return false;
            }

            LogMessage?.Invoke("Connected to server.");
            _ = Task.Run(() => ListenAsync(cancellationToken), cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke($"Connection error: {ex.Message}");
            return false;
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (IsConnected && !cancellationToken.IsCancellationRequested)
            {
                var message = await _reader!.ReadMessageAsync(cancellationToken);

                switch (message.Type)
                {
                    case MessageType.ServerMatchPrepare:
                        MatchPreparing?.Invoke(message.GetText());
                        break;
                    case MessageType.ServerGameStart:
                        var parts = message.GetText().Split('|');
                        var opponent = parts.ElementAtOrDefault(0) ?? "Unknown";
                        var color = parts.Length > 1 && Enum.TryParse<Color>(parts[1], out var parsedColor)
                            ? parsedColor
                            : Color.White;
                        GameStarted?.Invoke(opponent, color);
                        break;
                    case MessageType.ServerRequestMove:
                        await HandleMoveRequestAsync(message.Payload, cancellationToken);
                        break;
                    case MessageType.ServerGameEnd:
                        GameEnded?.Invoke(message.GetText());
                        break;
                    case MessageType.ServerError:
                        ErrorReceived?.Invoke(message.GetText());
                        break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException)
        {
            ErrorReceived?.Invoke("Server disconnected.");
        }
        catch (SocketException)
        {
            ErrorReceived?.Invoke("Connection lost.");
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke($"Network error: {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }

    private async Task HandleMoveRequestAsync(byte[] payload, CancellationToken cancellationToken)
    {
        if (_localBot == null)
        {
            await SendErrorAsync("Bot not initialized", cancellationToken);
            return;
        }

        var payloadText = Encoding.UTF8.GetString(payload);
        var lines = payloadText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var moves = new List<Move>();

        if (lines.Length > 0 && int.TryParse(lines[0], out var moveCount))
        {
            var moveStrings = lines.Skip(1).Take(moveCount);
            int index = 0;
            foreach (var move in moveStrings)
            {
                if (TryParseMove(move.Trim(), index % 2 == 0 ? Color.White : Color.Black, out var parsed))
                {
                    moves.Add(parsed);
                }
                index++;
            }
        }

        string resultMove;
        try
        {
            resultMove = _localBot.GetMove(moves);
        }
        catch (Exception ex)
        {
            await SendErrorAsync($"Bot execution error: {ex.Message}", cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(resultMove))
        {
            await SendErrorAsync("Bot returned empty move", cancellationToken);
            return;
        }

        var response = NetworkMessage.CreateText(MessageType.ClientMoveResponse, resultMove.Trim());
        await SendAsync(response, cancellationToken);
    }

    private static bool TryParseMove(string moveString, Color color, out Move move)
    {
        move = default;
        if (string.IsNullOrWhiteSpace(moveString) || moveString.Length < 3)
            return false;

        char pieceChar = moveString[0];
        string squareString = moveString.Substring(1).ToUpperInvariant();

        if (!Enum.TryParse<Square>(squareString, out var square))
            return false;

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
            return false;

        move = new Move(piece, square);
        return true;
    }

    private async Task SendErrorAsync(string error, CancellationToken cancellationToken)
    {
        ErrorReceived?.Invoke(error);
        var message = NetworkMessage.CreateText(MessageType.ClientDisconnect, error);
        await SendAsync(message, cancellationToken);
    }

    private async Task SendAsync(NetworkMessage message, CancellationToken cancellationToken)
    {
        if (_stream == null)
            return;

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

    public void Disconnect()
    {
        try { _tcpClient?.Close(); } catch { }
    }

    public void Dispose()
    {
        try { _sendLock.Dispose(); } catch { }
        try { _stream?.Dispose(); } catch { }
        try { _tcpClient?.Dispose(); } catch { }
    }
}
