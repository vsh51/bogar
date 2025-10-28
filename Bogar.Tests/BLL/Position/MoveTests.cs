using Bogar.BLL.Core;
using Xunit;

namespace Bogar.Tests.BLL.Core;

public class MoveTests
{
    [Fact]
    public void MoveToStringTest()
    {
        var piece = PieceExtensions.MakePiece(Color.Black, PieceType.Queen);

        var move = new Move(
            PieceExtensions.MakePiece(Color.Black, PieceType.Queen),
            Square.F4
        );

        Assert.Equal("QF4", move.ToString());
    }
}
