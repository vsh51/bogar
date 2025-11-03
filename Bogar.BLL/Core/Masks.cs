using System;

namespace Bogar.BLL.Core;

public static class Masks
{
    public static ulong PawnAttackMask(Square target, ulong occupied, Color color)
    {
        return Patterns.PawnAttackPattern[(int)color](target) & occupied;
    }

    public static ulong KnightAttackMask(Square target)
        => Patterns.KnightAttackPattern(target);

    public static ulong KingAttackMask(Square target)
        => Patterns.KingAttackPattern(target);

    public static ulong RookAttackMask(Square target, ulong occupied)
    {
        ulong rayWestOccupied = (Rays.GetRayWest(target) & occupied) | 0x01UL;
        ulong rayEastOccupied = (Rays.GetRayEast(target) & occupied) | 0x8000000000000000UL;
        ulong rayNorthOccupied = (Rays.GetRayNorth(target) & occupied) | 0x8000000000000000UL;
        ulong raySouthOccupied = (Rays.GetRaySouth(target) & occupied) | 0x01UL;

        int nearestWest = Bitboard.BitScanReverse(rayWestOccupied);
        int nearestEast = Bitboard.BitScanForward(rayEastOccupied);
        int nearestNorth = Bitboard.BitScanForward(rayNorthOccupied);
        int nearestSouth = Bitboard.BitScanReverse(raySouthOccupied);

        ulong westMask = Rays.GetRayWest(target) & ~Rays.GetRayWest((Square)nearestWest);
        ulong eastMask = Rays.GetRayEast(target) & ~Rays.GetRayEast((Square)nearestEast);
        ulong northMask = Rays.GetRayNorth(target) & ~Rays.GetRayNorth((Square)nearestNorth);
        ulong southMask = Rays.GetRaySouth(target) & ~Rays.GetRaySouth((Square)nearestSouth);

        return westMask | eastMask | northMask | southMask;
    }

    public static ulong BishopAttackMask(Square target, ulong occupied)
    {
        ulong neRay = Rays.GetRayNorthEast(target);
        ulong nwRay = Rays.GetRayNorthWest(target);
        ulong seRay = Rays.GetRaySouthEast(target);
        ulong swRay = Rays.GetRaySouthWest(target);

        int nearestNE = Bitboard.BitScanForward((neRay & occupied) | 0x8000000000000000UL);
        int nearestNW = Bitboard.BitScanForward((nwRay & occupied) | 0x8000000000000000UL);
        int nearestSE = Bitboard.BitScanReverse((seRay & occupied) | 0x01UL);
        int nearestSW = Bitboard.BitScanReverse((swRay & occupied) | 0x01UL);

        neRay &= ~Rays.GetRayNorthEast((Square)nearestNE);
        nwRay &= ~Rays.GetRayNorthWest((Square)nearestNW);
        seRay &= ~Rays.GetRaySouthEast((Square)nearestSE);
        swRay &= ~Rays.GetRaySouthWest((Square)nearestSW);

        return neRay | nwRay | seRay | swRay;
    }

    public static ulong QueenAttackMask(Square target, ulong occupied)
        => RookAttackMask(target, occupied) | BishopAttackMask(target, occupied);
}
