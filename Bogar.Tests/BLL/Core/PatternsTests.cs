using Bogar.BLL.Core;
using Xunit;

namespace Bogar.Tests.BLL.Core;

public class PatternsTests
{
    [Fact]
    public void PawnAttackPattern_Test()
    {
        Assert.Equal(0x50000UL, Patterns.WhitePawnAttackPattern(Square.B2));

        Assert.Equal(0x05UL, Patterns.BlackPawnAttackPattern(Square.B2));

        Assert.Equal(0x40000000000000UL, Patterns.WhitePawnAttackPattern(Square.H6));
        Assert.Equal(0x02UL, Patterns.BlackPawnAttackPattern(Square.A2));

        Assert.Equal(
            Patterns.WhitePawnAttackPattern(Square.C3),
            Patterns.PawnAttackPattern[(int)Color.White](Square.C3)
        );
    }

    [Fact]
    public void KnightAttackPattern_Test()
    {
        Assert.Equal(0x20400UL, Patterns.KnightAttackPattern(Square.A1));
        Assert.Equal(0x14220022140000UL, Patterns.KnightAttackPattern(Square.D5));
    }

    [Fact]
    public void KingAttackPattern_Test()
    {
        Assert.Equal(0x302UL, Patterns.KingAttackPattern(Square.A1));
        Assert.Equal(0x705070000000UL, Patterns.KingAttackPattern(Square.F5));
    }

    [Fact]
    public void BishopAttackPattern_Test()
    {
        Assert.Equal(0x8040201008040200UL, Patterns.BishopAttackPattern(Square.A1));
        Assert.Equal(0x2040810204080UL, Patterns.BishopAttackPattern(Square.A8));
    }

    [Fact]
    public void RookAttackPattern_Test()
    {
        Assert.Equal(0x1010101010101FEUL, Patterns.RookAttackPattern(Square.A1));
        Assert.Equal(0x202020DF20202020UL, Patterns.RookAttackPattern(Square.F5));
    }

    [Fact]
    public void QueenAttackPattern_Test()
    {
        Assert.Equal(
            Patterns.RookAttackPattern(Square.F5) | Patterns.BishopAttackPattern(Square.F5),
            Patterns.QueenAttackPattern(Square.F5)
        );
    }
}
