using Bogar.BLL.Core;
using Xunit;

namespace Bogar.Tests.BLL.Core;

public class MasksTests
{
    [Fact]
    public void PawnAttackMask_Test()
    {
        Assert.Equal(0x0UL, Masks.PawnAttackMask(Square.C2, 0x400UL, Color.White));
        Assert.Equal(
            0x500000000000UL,
            Masks.PawnAttackMask(Square.F7, 0x20500000000000UL, Color.Black)
        );
        Assert.Equal(0x20000UL, Masks.PawnAttackMask(Square.C2, 0x20400UL, Color.White));
        Assert.Equal(0x0UL, Masks.PawnAttackMask(Square.C2, 0x40400UL, Color.White));
    }

    [Fact]
    public void RookAttackMask_Test()
    {
        Assert.Equal(
            0xFE01010101010101UL,
            Masks.RookAttackMask(Square.A8, 0x100000000000000UL)
        );

        Assert.Equal(
            0x808080808080807FUL,
            Masks.RookAttackMask(Square.H1, 0x80UL)
        );

        Assert.Equal(
            0x8080808F7080808UL,
            Masks.RookAttackMask(Square.D4, 0x8000000UL)
        );
    }

    [Fact]
    public void BishopAttackMask_Test()
    {
        Assert.Equal(
            0x2040810204080UL,
            Masks.BishopAttackMask(Square.A8, 0x100000000000000UL)
        );

        Assert.Equal(
            0x40201008040201UL,
            Masks.BishopAttackMask(Square.H8, 0x8000000000000000UL)
        );

        Assert.Equal(
            0x8041221400142241UL,
            Masks.BishopAttackMask(Square.D4, 0x8000000UL)
        );
    }

    [Fact]
    public void QueenAttackMask_Test()
    {
        Assert.Equal(
            0x81412111090503FEUL,
            Masks.QueenAttackMask(Square.A1, 0x1UL)
        );

        Assert.Equal(
            0x40C0A09088848281UL,
            Masks.QueenAttackMask(Square.H8, 0xC000000000000000UL)
        );

        Assert.Equal(
            0x88492A1CF71C2A49UL,
            Masks.QueenAttackMask(Square.D4, 0x8000000UL)
        );
    }

    [Fact]
    public void KingAttackMask_Test()
    {
        Assert.Equal(
            Patterns.KingAttackPattern(Square.E4),
            Masks.KingAttackMask(Square.E4)
        );
    }

    [Fact]
    public void KnightAttackMask_Test()
    {
        Assert.Equal(
            Patterns.KnightAttackPattern(Square.D5),
            Masks.KnightAttackMask(Square.D5)
        );
    }
}
