namespace Bogar.BLL.Core;

public class Position
{
    private readonly Board _board;
    private Color _sideToMove;

    public Position()
    {
        this._board = new Board();
        this._sideToMove = Color.White;
    }

    public Position(Position other)
    {
        this._board = new Board(other._board);
        this._sideToMove = other._sideToMove;
    }

    public bool IsLegal(Move move)
    {
        if (move.Piece.ColorOfPiece() != this._sideToMove)
        {
            return false;
        }

        if ((this._board.Occupied() & move.Square.ToBitboard()) != 0)
        {
            return false;
        }

        PieceType type = move.Piece.TypeOfPiece();
        if (type == PieceType.NoPieceType)
        {
            return false;
        }

        if (!this.HasPiecesLeft(move.Piece))
        {
            return false;
        }

        return true;
    }

    private bool HasPiecesLeft(Piece piece)
    {
        Color color = piece.ColorOfPiece();
        PieceType type = piece.TypeOfPiece();

        ulong piecesOfType = this._board.GetPiecesBitboard(type);
        ulong piecesOfColor = this._board.GetColorBitboard(color);
        ulong piecesOfThisKind = piecesOfType & piecesOfColor;
        int count = Bitboard.PopCount(piecesOfThisKind);

        int maxCount = GetMaxPieceCount(type);
        return count < maxCount;
    }

    private static int GetMaxPieceCount(PieceType type)
    {
        return type switch
        {
            PieceType.Pawn => 8,
            PieceType.Knight => 2,
            PieceType.Bishop => 2,
            PieceType.Rook => 2,
            PieceType.Queen => 1,
            PieceType.King => 1,
            _ => 0
        };
    }

    public void DoMove(Move move)
    {
        if (!this.IsLegal(move))
        {
            throw new InvalidOperationException($"Illegal move: {move}");
        }

        this._board.PlacePiece(move.Piece, move.Square);
        this._sideToMove = this._sideToMove == Color.White ? Color.Black : Color.White;
    }

    public (int whiteScore, int blackScore) CalculateScore()
    {
        int whiteScore = 0;
        int blackScore = 0;

        ulong occupied = this._board.Occupied();

        for (Square sq = Square.A1; sq < Square.SQ_COUNT; sq++)
        {
            ulong sqBitboard = sq.ToBitboard();
            if ((occupied & sqBitboard) == 0)
            {
                continue;
            }

            Piece piece = this._board.PieceAt(sq);

            Color color = piece.ColorOfPiece();
            PieceType type = piece.TypeOfPiece();
            int value = GetPieceValue(type);
            int attacks = this._board.CountAttacksOn(sq);
            int points = value * attacks;

            if (color == Color.White)
            {
                blackScore += points;
            }
            else
            {
                whiteScore += points;
            }
        }

        return (whiteScore, blackScore);
    }

    private static int GetPieceValue(PieceType type)
    {
        return type switch
        {
            PieceType.Pawn => 1,
            PieceType.Knight => 3,
            PieceType.Bishop => 3,
            PieceType.Rook => 5,
            PieceType.Queen => 8,
            PieceType.King => 9,
            _ => 0
        };
    }

    public Board GetBoard() => this._board;
}
