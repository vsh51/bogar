namespace Bogar.BLL.Core;

public readonly struct Move
{
    public Piece Piece { get; }
    public Square Square { get; }

    public Move(Piece piece, Square square)
    {
        this.Piece = piece;
        this.Square = square;
    }

    public override string ToString()
    {
        char symbol = this.Piece.TypeOfPiece() switch
        {
            PieceType.Pawn => 'P',
            PieceType.Knight => 'N',
            PieceType.Bishop => 'B',
            PieceType.Rook => 'R',
            PieceType.Queen => 'Q',
            PieceType.King => 'K',
            _ => '?'
        };

        return $"{symbol}{this.Square}";
    }
}
