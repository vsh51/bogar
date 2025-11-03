using System;
using System.Diagnostics;

namespace Bogar.BLL.Core;

public static class Rays
{
    public static ulong GetRayBetween(Square from, Square to)
    {
        Debug.Assert(from >= 0 && from < Square.SQ_COUNT);
        Debug.Assert(to >= 0 && to < Square.SQ_COUNT);

        const ulong m1 = ~0UL;
        const ulong a2a7 = 0x0001010101010100UL;
        const ulong b2g7 = 0x0040201008040200UL;
        const ulong h1b7 = 0x0002040810204080UL;

        ulong between, line, rank, file;

        between = (m1 << (int)from) ^ (m1 << (int)to);
        file = ((ulong)to & 7) - ((ulong)from & 7);
        rank = (((ulong)to | 7) - (ulong)from) >> 3;
        line = ((file & 7) - 1) & a2a7;
        line += 2 * (((rank & 7) - 1) >> 58);
        line += (((rank - file) & 15) - 1) & b2g7;
        line += (((rank + file) & 15) - 1) & h1b7;
        line *= between & (~between + 1);
        return line & between;
    }

    public static ulong GetRayWest(Square square)
    {
        Debug.Assert(square >= 0 && square < Square.SQ_COUNT);
        return (1UL << (int)square) - (1UL << ((int)square & 56));
    }

    public static ulong GetRaySouth(Square square)
    {
        Debug.Assert(square >= 0 && square < Square.SQ_COUNT);
        return 0x0080808080808080UL >> ((int)square ^ 63);
    }

    public static ulong GetRayEast(Square square)
    {
        Debug.Assert(square >= 0 && square < Square.SQ_COUNT);
        return 2UL * ((1UL << ((int)square | 7)) - (1UL << (int)square));
    }

    public static ulong GetRayNorth(Square square)
    {
        Debug.Assert(square >= 0 && square < Square.SQ_COUNT);
        return 0x0101010101010100UL << (int)square;
    }

    public static ulong GetRayDiagonal(Square square)
    {
        Debug.Assert(square >= 0 && square < Square.SQ_COUNT);

        int diagonal = 8 * ((int)square & 7) - ((int)square & 56);
        int nort = -diagonal & (diagonal >> 31);
        int sout = diagonal & (-diagonal >> 31);

        return (0x8040201008040201UL >> sout) << nort;
    }

    public static ulong GetRayAntiDiagonal(Square square)
    {
        Debug.Assert(square >= 0 && square < Square.SQ_COUNT);

        int diagonal = 56 - 8 * ((int)square & 7) - ((int)square & 56);
        int north = -diagonal & (diagonal >> 31);
        int south = diagonal & (-diagonal >> 31);

        return (0x0102040810204080UL >> south) << north;
    }

    public static ulong GetRayNorthEast(Square square)
    {
        Debug.Assert(square >= 0 && square < Square.SQ_COUNT);
        return GetRayDiagonal(square) & (~1UL << (int)square);
    }

    public static ulong GetRaySouthWest(Square square)
    {
        Debug.Assert(square >= 0 && square < Square.SQ_COUNT);
        return GetRayDiagonal(square) & ((1UL << (int)square) - 1);
    }

    public static ulong GetRayNorthWest(Square square)
    {
        Debug.Assert(square >= 0 && square < Square.SQ_COUNT);
        return GetRayAntiDiagonal(square) & (~1UL << (int)square);
    }

    public static ulong GetRaySouthEast(Square square)
    {
        Debug.Assert(square >= 0 && square < Square.SQ_COUNT);
        return GetRayAntiDiagonal(square) & ((1UL << (int)square) - 1);
    }
}
