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

public static class PieceUtils
{
    public static PieceType TypeOfPiece(Piece piece)
        => (PieceType)((int)piece & 7);

    public static Color ColorOfPiece(Piece piece)
        => (int)piece > 8 ? Color.Black : Color.White;

    public static Piece MakePiece(Color color, PieceType pieceType)
        => (Piece)(((int)color << 3) + (int)pieceType);
}
