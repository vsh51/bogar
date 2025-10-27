using Bogar.BLL.Core;
using Xunit;

namespace Bogar.Tests.BLL.Core;

public class RaysTests
{
    [Fact]
    public void GetRayWestTest()
    {
        Assert.Equal(0x0000000000030000UL, Rays.GetRayWest(Square.C3));
        Assert.Equal(0x0000000000000000UL, Rays.GetRayWest(Square.A1));
        Assert.Equal(0x7F00000000000000UL, Rays.GetRayWest(Square.H8));
    }

    [Fact]
    public void GetRayEastTest()
    {
        Assert.Equal(0xFE00000000000000UL, Rays.GetRayEast(Square.A8));
        Assert.Equal(0x00000000F0000000UL, Rays.GetRayEast(Square.D4));
        Assert.Equal(0x0000000000000000UL, Rays.GetRayEast(Square.H1));
    }

    [Fact]
    public void GetRayNorthTest()
    {
        Assert.Equal(0x0000000000000000UL, Rays.GetRayNorth(Square.C8));
        Assert.Equal(0x2020202020200000UL, Rays.GetRayNorth(Square.F2));
        Assert.Equal(0x0101010101010100UL, Rays.GetRayNorth(Square.A1));
    }

    [Fact]
    public void GetRaySouthTest()
    {
        Assert.Equal(0x0000000000000000UL, Rays.GetRaySouth(Square.A1));
        Assert.Equal(0x0000000000000202UL, Rays.GetRaySouth(Square.B3));
        Assert.Equal(0x0080808080808080UL, Rays.GetRaySouth(Square.H8));
    }

    [Fact]
    public void GetRayDiagonalTest()
    {
        Assert.Equal(0x080402010080402UL, Rays.GetRayDiagonal(Square.B1));
        Assert.Equal(0x0100000000000000UL, Rays.GetRayDiagonal(Square.A8));
        Assert.Equal(0x0000000000000080UL, Rays.GetRayDiagonal(Square.H1));
    }

    [Fact]
    public void GetRayAntiDiagonalTest()
    {
        Assert.Equal(0x8000000000000000UL, Rays.GetRayAntiDiagonal(Square.H8));
        Assert.Equal(0x0000000000000001UL, Rays.GetRayAntiDiagonal(Square.A1));
        Assert.Equal(0x0102040810204080UL, Rays.GetRayAntiDiagonal(Square.A8));
    }

    [Fact]
    public void GetRaySouthWestTest()
    {
        Assert.Equal(0x0000000000000000UL, Rays.GetRaySouthWest(Square.A1));
        Assert.Equal(0x0000000000402010UL, Rays.GetRaySouthWest(Square.H4));
        Assert.Equal(0x0000000000000000UL, Rays.GetRaySouthWest(Square.A8));
    }

    [Fact]
    public void GetRayNorthEastTest()
    {
        Assert.Equal(0x0000000000000000UL, Rays.GetRayNorthEast(Square.H1));
        Assert.Equal(0x8040201008040200UL, Rays.GetRayNorthEast(Square.A1));
        Assert.Equal(0x0000000000000000UL, Rays.GetRayNorthEast(Square.H8));
    }

    [Fact]
    public void GetRayNorthWestTest()
    {
        Assert.Equal(0x0000000000000000UL, Rays.GetRayNorthWest(Square.A1));
        Assert.Equal(0x0102040810204000UL, Rays.GetRayNorthWest(Square.H1));
        Assert.Equal(0x0000000000000000UL, Rays.GetRayNorthWest(Square.A8));
    }

    [Fact]
    public void GetRaySouthEastTest()
    {
        Assert.Equal(0x0000000000000000UL, Rays.GetRaySouthEast(Square.H8));
        Assert.Equal(0x0002040810204080UL, Rays.GetRaySouthEast(Square.A8));
        Assert.Equal(0x0000000000000000UL, Rays.GetRaySouthEast(Square.H1));
    }

    [Fact]
    public void GetRayBetweenTest()
    {
        Assert.Equal(0x000000000000007EUL, Rays.GetRayBetween(Square.A1, Square.H1));
        Assert.Equal(0x0000040404040000UL, Rays.GetRayBetween(Square.C2, Square.C7));
        Assert.Equal(0x0002040810204000UL, Rays.GetRayBetween(Square.A8, Square.H1));
        Assert.Equal(0x0000000008040200UL, Rays.GetRayBetween(Square.A1, Square.E5));

        var ab = Rays.GetRayBetween(Square.A3, Square.F3);
        var ba = Rays.GetRayBetween(Square.F3, Square.A3);
        Assert.Equal(ab, ba);
    }
}
