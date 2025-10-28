using Bogar.BLL.Core;
using Xunit;

namespace Bogar.Tests.BLL.Core;

public class PieceTests
{
    [Fact]
    public void MakePieceTest()
    {
        var piece = PieceExtensions.MakePiece(Color.Black, PieceType.Queen);
        Assert.Equal(Piece.BlackQueen, piece);
    }

    [Fact]
    public void ColorOfPieceTest()
    {
        Assert.Equal(Color.White, Piece.WhitePawn.ColorOfPiece());
        Assert.Equal(Color.Black, Piece.BlackRook.ColorOfPiece());
    }

    [Fact]
    public void TypeOfPieceTest()
    {
        Assert.Equal(PieceType.King, Piece.BlackKing.TypeOfPiece());
        Assert.Equal(PieceType.Pawn, Piece.WhitePawn.TypeOfPiece());
    }

    [Fact]
    public void NoPieceTest()
    {
        Assert.Equal(PieceType.NoPieceType, Piece.NoPiece.TypeOfPiece());
        Assert.Equal(Color.White, Piece.NoPiece.ColorOfPiece());
    }
}
