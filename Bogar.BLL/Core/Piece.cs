namespace Bogar.BLL.Core;

public enum Color : int
{
    White = 0,
    Black = 1,
    ColorNb = 2
}

public enum PieceType : int
{
    NoPieceType = 0,
    Pawn = 1,
    Knight,
    Bishop,
    Rook,
    Queen,
    King,

    PieceTypeNb = 6
}

public enum Piece : int
{
    NoPiece = 0,

    WhitePawn = 1,
    WhiteKnight,
    WhiteBishop,
    WhiteRook,
    WhiteQueen,
    WhiteKing,

    BlackPawn = 9,
    BlackKnight,
    BlackBishop,
    BlackRook,
    BlackQueen,
    BlackKing,

    PieceNb = 12
}

public static class PieceExtensions
{
    public static Color ColorOfPiece(this Piece piece)
    {
        if (piece == Piece.NoPiece)
        {
            return Color.White;
        }

        return (int)piece > 8 ? Color.Black : Color.White;
    }

    public static PieceType TypeOfPiece(this Piece piece)
    {
        if (piece == Piece.NoPiece)
        {
            return PieceType.NoPieceType;
        }

        return (PieceType)((int)piece & 7);
    }

    public static Piece MakePiece(Color color, PieceType type)
    {
        if (type == PieceType.NoPieceType)
        {
            return Piece.NoPiece;
        }

        return (Piece)(((int)color << 3) + (int)type);
    }
}
