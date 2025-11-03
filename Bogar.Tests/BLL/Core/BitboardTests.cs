using Bogar.BLL.Core;
using Xunit;

namespace Bogar.Tests.BLL.Core;

public class BitboardTests
{
    [Fact]
    public void BitScanForwardTest()
    {
        Assert.Equal(0, Bitboard.BitScanForward(Bitboard.UNIVERSE));
        Assert.Equal(4, Bitboard.BitScanForward(0x10UL));
        Assert.Equal(56, Bitboard.BitScanForward(0x0100000000000000UL));
    }

    [Fact]
    public void BitScanReverseTest()
    {
        Assert.Equal(63, Bitboard.BitScanReverse(Bitboard.UNIVERSE));
        Assert.Equal(4, Bitboard.BitScanReverse(0x10UL));
        Assert.Equal(56, Bitboard.BitScanReverse(0x0100000000000000UL));
    }

    [Fact]
    public void PopCountTest()
    {
        Assert.Equal(0, Bitboard.PopCount(Bitboard.EMPTY));
        Assert.Equal(64, Bitboard.PopCount(Bitboard.UNIVERSE));
        Assert.Equal(8, Bitboard.PopCount(0xFFUL));
        Assert.Equal(4, Bitboard.PopCount(0x0000F0UL));
    }
}
