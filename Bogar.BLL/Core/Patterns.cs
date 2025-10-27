using System;

namespace Bogar.BLL.Core;

public static class Patterns
{
    private const ulong FILE_A = 0x0101010101010101UL;
    private const ulong FILE_B = 0x0202020202020202UL;
    private const ulong FILE_G = 0x4040404040404040UL;
    private const ulong FILE_H = 0x8080808080808080UL;

    private const ulong RANK_2 = 0x000000000000FF00UL;
    private const ulong RANK_7 = 0x00FF000000000000UL;

    public static ulong WhitePawnAttackPattern(Square target)
    {
        ulong bb = target.ToBitboard();
        return ((bb & ~FILE_H) << 9) | ((bb & ~FILE_A) << 7);
    }

    public static ulong BlackPawnAttackPattern(Square target)
    {
        ulong bb = target.ToBitboard();
        return ((bb & ~FILE_A) >> 9) | ((bb & ~FILE_H) >> 7);
    }

    public static readonly Func<Square, ulong>[] PawnAttackPattern = new Func<Square, ulong>[]
    {
        WhitePawnAttackPattern,
        BlackPawnAttackPattern
    };

    public static ulong KnightAttackPattern(Square target)
    {
        ulong bb = target.ToBitboard();

        ulong l1 = (bb >> 1) & ~FILE_H;
        ulong l2 = (bb >> 2) & ~(FILE_H | FILE_G);
        ulong r1 = (bb << 1) & ~FILE_A;
        ulong r2 = (bb << 2) & ~(FILE_A | FILE_B);

        return ((l1 | r1) << 16)
             | ((l2 | r2) << 8)
             | ((l1 | r1) >> 16)
             | ((l2 | r2) >> 8);
    }

    public static ulong KingAttackPattern(Square target)
    {
        ulong bb = target.ToBitboard();

        return (bb << 8)
             | (bb >> 8)
             | ((bb << 1) & ~FILE_A)
             | ((bb >> 1) & ~FILE_H)
             | ((bb << 9) & ~FILE_A)
             | ((bb << 7) & ~FILE_H)
             | ((bb >> 9) & ~FILE_H)
             | ((bb >> 7) & ~FILE_A);
    }

    public static ulong RookAttackPattern(Square target)
    {
        ulong vertical = target.ToBitboard() | Rays.GetRayNorth(target) | Rays.GetRaySouth(target);
        ulong horizontal = target.ToBitboard() | Rays.GetRayEast(target) | Rays.GetRayWest(target);

        return vertical ^ horizontal;
    }

    public static ulong BishopAttackPattern(Square target)
    {
        ulong diag = Rays.GetRayDiagonal(target);
        ulong anti = Rays.GetRayAntiDiagonal(target);
        return diag ^ anti;
    }

    public static ulong QueenAttackPattern(Square target)
        => RookAttackPattern(target) | BishopAttackPattern(target);
}
