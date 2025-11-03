using Bogar.BLL.Core;
using Xunit;

namespace Bogar.Tests.BLL.Core;

public class SquareTests
{
    [Fact]
    public void ToBitboard_SetsCorrectBit()
    {
        ulong a1 = Square.A1.ToBitboard();
        ulong e4 = Square.E4.ToBitboard();
        ulong h8 = Square.H8.ToBitboard();

        Assert.Equal(1UL << 0, a1);
        Assert.Equal(1UL << 28, e4);
        Assert.Equal(1UL << 63, h8);
    }

    [Fact]
    public void ToSquare_ReturnsCorrectSquare()
    {
        Assert.Equal(Square.A1, (1UL << 0).ToSquare());
        Assert.Equal(Square.E4, (1UL << 28).ToSquare());
        Assert.Equal(Square.H8, (1UL << 63).ToSquare());
    }

    [Fact]
    public void GetFile_And_GetRank_WorkCorrectly()
    {
        Assert.Equal(0, Square.A1.GetFile());
        Assert.Equal(7, Square.H1.GetFile());
        Assert.Equal(0, Square.A1.GetRank());
        Assert.Equal(7, Square.A8.GetRank());
        Assert.Equal(4, Square.E4.GetFile());
        Assert.Equal(3, Square.E4.GetRank());
    }

    [Fact]
    public void ToAlgebraic_ReturnsCorrectString()
    {
        Assert.Equal("a1", Square.A1.ToAlgebraic());
        Assert.Equal("e4", Square.E4.ToAlgebraic());
        Assert.Equal("h8", Square.H8.ToAlgebraic());
        Assert.Equal("none", Square.SQ_NONE.ToAlgebraic());
    }

    [Theory]
    [InlineData("a1", Square.A1)]
    [InlineData("e4", Square.E4)]
    [InlineData("h8", Square.H8)]
    [InlineData("A1", Square.A1)]
    [InlineData("E4", Square.E4)]
    [InlineData("z9", Square.SQ_NONE)]
    [InlineData("e9", Square.SQ_NONE)]
    [InlineData("aa", Square.SQ_NONE)]
    [InlineData("", Square.SQ_NONE)]
    public void Parse_WorksCorrectly(string input, Square expected)
    {
        Assert.Equal(expected, SquareExtensions.Parse(input));
    }
}
