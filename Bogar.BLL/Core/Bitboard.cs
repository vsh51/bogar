namespace Bogar.BLL.Core;

public static class Bitboard
{
    public const ulong EMPTY = 0x0UL;
    public const ulong UNIVERSE = 0xFFFFFFFFFFFFFFFFUL;

    private static readonly uint[] BitScanIndexes =
    {
        0, 47, 1, 56, 48, 27, 2, 60,
        57, 49, 41, 37, 28, 16, 3, 61,
        54, 58, 35, 52, 50, 42, 21, 44,
        38, 32, 29, 23, 17, 11, 4, 62,
        46, 55, 26, 59, 40, 36, 15, 53,
        34, 51, 20, 43, 31, 22, 10, 45,
        25, 39, 14, 33, 19, 30, 9, 24,
        13, 18, 8, 12, 7, 6, 5, 63
    };

    public static int BitScanForward(ulong bitboard)
    {
        if (bitboard == EMPTY)
        {
            return -1;
        }

        ulong debruijn = 0x03f79d71b4cb0a89UL;
        return (int)BitScanIndexes[
            ((bitboard ^ (bitboard - 1)) * debruijn) >> 58
        ];
    }

    public static int BitScanReverse(ulong bitboard)
    {
        if (bitboard == EMPTY)
        {
            return -1;
        }

        bitboard |= bitboard >> 1;
        bitboard |= bitboard >> 2;
        bitboard |= bitboard >> 4;
        bitboard |= bitboard >> 8;
        bitboard |= bitboard >> 16;
        bitboard |= bitboard >> 32;
        ulong debruijn = 0x03f79d71b4cb0a89UL;
        return (int)BitScanIndexes[(bitboard * debruijn) >> 58];
    }

    public static int PopCount(ulong bitboard)
    {
        int count = 0;
        while (bitboard != EMPTY)
        {
            bitboard &= bitboard - 1;
            count++;
        }
        return count;
    }
}
