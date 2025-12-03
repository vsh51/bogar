namespace Bogar.BLL.Core;

public class Board
{
    private readonly ulong[] _pieces;
    private readonly ulong[] _colors;

    public Board()
    {
        this._pieces = new ulong[(int)PieceType.PieceTypeNb + 1];
        this._colors = new ulong[(int)Color.ColorNb];
    }

    public Board(Board other)
    {
        this._pieces = new ulong[(int)PieceType.PieceTypeNb + 1];
        this._colors = new ulong[(int)Color.ColorNb];

        Array.Copy(other._pieces, this._pieces, this._pieces.Length);
        Array.Copy(other._colors, this._colors, this._colors.Length);
    }

    public Piece PieceAt(Square square)
    {
        if (square < 0 || square >= Square.SQ_COUNT)
        {
            return Piece.NoPiece;
        }

        ulong mask = square.ToBitboard();

        if ((this.Occupied() & mask) == 0)
        {
            return Piece.NoPiece;
        }

        Color color = (this._colors[(int)Color.White] & mask) != 0 ? Color.White : Color.Black;

        for (PieceType pt = PieceType.Pawn; pt <= PieceType.PieceTypeNb; pt++)
        {
            if ((this._pieces[(int)pt] & mask) != 0)
            {
                Piece result = PieceExtensions.MakePiece(color, pt);
                return result;
            }
        }

        return Piece.NoPiece;
    }

    public ulong Occupied() => this._colors[(int)Color.White] | this._colors[(int)Color.Black];

    public ulong GetPiecesBitboard(PieceType type) => this._pieces[(int)type];

    public ulong GetColorBitboard(Color color) => this._colors[(int)color];

    public void PlacePiece(Piece piece, Square square)
    {
        if (piece == Piece.NoPiece || square < 0 || square >= Square.SQ_COUNT)
        {
            return;
        }

        ulong mask = square.ToBitboard();
        Color color = piece.ColorOfPiece();
        PieceType type = piece.TypeOfPiece();

        this._pieces[(int)type] |= mask;
        this._colors[(int)color] |= mask;
    }

    public void RemovePiece(Square square)
    {
        if (square < 0 || square >= Square.SQ_COUNT)
        {
            return;
        }

        ulong mask = ~square.ToBitboard();

        for (int i = 0; i < this._pieces.Length; i++)
        {
            this._pieces[i] &= mask;
        }

        for (int i = 0; i < this._colors.Length; i++)
        {
            this._colors[i] &= mask;
        }
    }

    public int CountAttacksOn(Square target)
    {
        Piece targetPiece = this.PieceAt(target);
        if (targetPiece == Piece.NoPiece)
        {
            return 0;
        }

        Color targetColor = targetPiece.ColorOfPiece();
        Color enemyColor = targetColor == Color.White ? Color.Black : Color.White;

        ulong occupied = this.Occupied();
        ulong enemyPieces = this._colors[(int)enemyColor];
        int attacks = 0;

        ulong enemyPawns = this._pieces[(int)PieceType.Pawn] & enemyPieces;
        ulong pawnAttackers = Masks.PawnAttackMask(target, enemyPawns, targetColor);
        int pawnAttackCount = Bitboard.PopCount(pawnAttackers);
        attacks += pawnAttackCount;

        ulong knightAttackers = Masks.KnightAttackMask(target) & this._pieces[(int)PieceType.Knight] & enemyPieces;
        int knightAttackCount = Bitboard.PopCount(knightAttackers);
        attacks += knightAttackCount;

        ulong bishopAttackers = Masks.BishopAttackMask(target, occupied) & this._pieces[(int)PieceType.Bishop] & enemyPieces;
        int bishopAttackCount = Bitboard.PopCount(bishopAttackers);
        attacks += bishopAttackCount;

        ulong rookAttackers = Masks.RookAttackMask(target, occupied) & this._pieces[(int)PieceType.Rook] & enemyPieces;
        int rookAttackCount = Bitboard.PopCount(rookAttackers);
        attacks += rookAttackCount;

        ulong queenAttackers = Masks.QueenAttackMask(target, occupied) & this._pieces[(int)PieceType.Queen] & enemyPieces;
        int queenAttackCount = Bitboard.PopCount(queenAttackers);
        attacks += queenAttackCount;

        ulong kingAttackers = Masks.KingAttackMask(target) & this._pieces[(int)PieceType.King] & enemyPieces;
        int kingAttackCount = Bitboard.PopCount(kingAttackers);
        attacks += kingAttackCount;

        return attacks;
    }
}
