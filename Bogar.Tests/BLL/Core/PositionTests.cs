using Bogar.BLL.Core;
using Xunit;

namespace Bogar.Tests.BLL.Core
{
    public class PositionTests
    {
        [Fact]
        public void CanInstantiatePositionTest()
        {
            var pos = new Position();
            Assert.NotNull(pos);
        }

        [Fact]
        public void CannotPlaceOpponentPieceOnFirstMoveTest()
        {
            var pos = new Position();
            var illegalMove = new Move(Piece.BlackPawn, Square.A2);
            Assert.False(pos.IsLegal(illegalMove));
        }

        [Fact]
        public void PlacePieceTogglesTurnAndPreventsDuplicateColourTest()
        {
            var pos = new Position();

            var whiteMove = new Move(Piece.WhitePawn, Square.A2);
            Assert.True(pos.IsLegal(whiteMove));
            pos.DoMove(whiteMove);

            var illegalWhiteMove = new Move(Piece.WhiteKnight, Square.B2);
            Assert.False(pos.IsLegal(illegalWhiteMove));

            var blackMove = new Move(Piece.BlackKnight, Square.B2);
            Assert.True(pos.IsLegal(blackMove));

            pos.DoMove(new Move(Piece.BlackRook, Square.F3));
            pos.DoMove(new Move(Piece.WhiteQueen, Square.C3));
            pos.DoMove(new Move(Piece.BlackRook, Square.F4));

            var twoQueens = new Move(Piece.WhiteQueen, Square.A1);
            Assert.False(pos.IsLegal(twoQueens));
        }

        [Fact]
        public void CannotPlaceOnOccupiedSquareTest()
        {
            var pos = new Position();

            var firstMove = new Move(Piece.WhiteKnight, Square.C3);
            Assert.True(pos.IsLegal(firstMove));
            pos.DoMove(firstMove);

            var secondMove = new Move(Piece.BlackBishop, Square.C3);
            Assert.False(pos.IsLegal(secondMove));
        }

        [Fact]
        public void ScoreCalculationTest()
        {
            // FEN: 8/8/1p3k2/B3P3/1p5B/2Q2r2/8/P7 b - - 0 1
            var pos = new Position();

            pos.DoMove(new Move(Piece.WhiteQueen, Square.C3));
            pos.DoMove(new Move(Piece.BlackRook, Square.F3));
            pos.DoMove(new Move(Piece.WhitePawn, Square.E5));
            pos.DoMove(new Move(Piece.BlackKing, Square.F6));
            pos.DoMove(new Move(Piece.WhitePawn, Square.A1));
            pos.DoMove(new Move(Piece.BlackPawn, Square.B4));
            pos.DoMove(new Move(Piece.WhiteBishop, Square.A5));
            pos.DoMove(new Move(Piece.BlackPawn, Square.B6));
            pos.DoMove(new Move(Piece.WhiteBishop, Square.H4));

            var score = pos.CalculateScore();

            Assert.Equal(26, score.whiteScore);
            Assert.Equal(20, score.blackScore);
        }
    }
}
