using Bogar.BLL.Core;
using Xunit;

namespace Bogar.Tests.BLL.Core;

public class PieceTests
{
    [Fact]
    public void MakePieceTest()
    {
        var piece = PieceUtils.MakePiece(Color.Black, PieceType.Queen);
        Assert.Equal(Piece.BlackQueen, piece);
    }

    [Fact]
    public void ColorOfPieceTest()
    {
        Assert.Equal(Color.White, PieceUtils.ColorOfPiece(Piece.WhitePawn));
        Assert.Equal(Color.Black, PieceUtils.ColorOfPiece(Piece.BlackRook));
    }

    [Fact]
    public void TypeOfPieceTest()
    {
        Assert.Equal(PieceType.King, PieceUtils.TypeOfPiece(Piece.BlackKing));
        Assert.Equal(PieceType.Pawn, PieceUtils.TypeOfPiece(Piece.WhitePawn));
    }

    [Fact]
    public void NoPieceTest()
    {
        Assert.Equal(PieceType.NoPieceType, PieceUtils.TypeOfPiece(Piece.NoPiece));
        Assert.Equal(Color.White, PieceUtils.ColorOfPiece(Piece.NoPiece));
    }
}
