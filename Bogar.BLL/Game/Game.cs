using Bogar.BLL.Core;
using Bogar.BLL.Player;
using System;
using System.Collections.Generic;
namespace Bogar.BLL.Game;

public class Game
{
    private readonly IPlayer _whitePlayer;
    private readonly IPlayer _blackPlayer;
    private readonly Position _position;

    public List<Move> Moves { get; private set; }

    public Game(IPlayer pl1, IPlayer pl2)
    {
        this._whitePlayer = pl1;
        this._blackPlayer = pl2;
        this._position = new Position();
        this.Moves = new List<Move>();
    }

    public int GetScore()
    {
        var score = this._position.CalculateScore();
        return score.whiteScore - score.blackScore;
    }

    public (int whiteScore, int blackScore) GetScoreBreakdown()
    {
        return this._position.CalculateScore();
    }

    public void DoNextMove()
    {
        if (this.Moves.Count >= 32)
        {
            throw new InvalidOperationException(
                "Game is over. Cannot make more moves.");
        }

        bool isWhiteTurn = (this.Moves.Count % 2 == 0);
        IPlayer currentPlayer = isWhiteTurn ? this._whitePlayer : this._blackPlayer;
        Color currentSide = isWhiteTurn ? Color.White : Color.Black;

        string moveString = currentPlayer.GetMove(this.Moves);
        if (string.IsNullOrEmpty(moveString))
        {
            throw new InvalidOperationException(
                $"Bot ({currentSide}) returned a null or empty move.");
        }

        Move newMove;
        try
        {
            newMove = this.ParseMoveString(moveString, currentSide);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Bot ({currentSide}) returned an invalid move string: '{moveString}'.",
                ex);
        }

        this._position.DoMove(newMove);

        this.Moves.Add(newMove);
    }

    public Position GetCurrentPosition()
    {
        return new Position(this._position);
    }

    public bool IsGameOver()
    {
        return this.Moves.Count >= 32;
    }

    public Color GetCurrentTurn()
    {
        return (this.Moves.Count % 2 == 0) ? Color.White : Color.Black;
    }

    private Move ParseMoveString(string moveString, Color sideToMove)
    {
        if (string.IsNullOrEmpty(moveString) || moveString.Length < 2)
        {
            throw new FormatException(
                $"Invalid move string format: {moveString}");
        }

        char pieceChar = moveString[0];

        string squareString = moveString.Substring(1).ToUpper();
        if (!Enum.TryParse<Square>(squareString, out Square square))
        {
            throw new FormatException(
                $"Invalid square in move: {squareString}");
        }

        Piece piece;
        if (sideToMove == Color.White)
        {
            piece = pieceChar switch
            {
                'P' => Piece.WhitePawn,
                'N' => Piece.WhiteKnight,
                'B' => Piece.WhiteBishop,
                'R' => Piece.WhiteRook,
                'Q' => Piece.WhiteQueen,
                'K' => Piece.WhiteKing,
                _ => throw new FormatException(
                    $"Invalid piece type in move: {pieceChar}")
            };
        }
        else
        {
            piece = pieceChar switch
            {
                'P' => Piece.BlackPawn,
                'N' => Piece.BlackKnight,
                'B' => Piece.BlackBishop,
                'R' => Piece.BlackRook,
                'Q' => Piece.BlackQueen,
                'K' => Piece.BlackKing,
                _ => throw new FormatException(
                    $"Invalid piece type in move: {pieceChar}")
            };
        }
        return new Move(piece, square);
    }
}
