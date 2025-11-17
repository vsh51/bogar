using Bogar.BLL.Player;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Bogar.BLL.Core; 
using BotPlayer = Bogar.BLL.Player.Player;
namespace Bogar.Tests.BLL.Player
{
    public class PlayerTests
    {
        private const string RealBotPath = @"..\..\..\test_bots\random_bot.exe";

        [Fact]
        public void GetMove_SendsMultipleMoves_ReturnsValidMove()
        {
            Assert.True(File.Exists(RealBotPath), $"Bot not found at {Path.GetFullPath(RealBotPath)}.");
            
            var player = new BotPlayer(RealBotPath);
            var position = new Position(); 
            var moves = new List<Move>
            {
                new Move(Piece.WhitePawn, Square.A2), 
                new Move(Piece.BlackPawn, Square.A7), 
                new Move(Piece.WhiteKnight, Square.C3) 
            };
            
            foreach (var move in moves)
            {
                position.DoMove(move);
            }
            
            string botResponse = player.GetMove(moves);
            
            Assert.False(string.IsNullOrEmpty(botResponse), "Bot returned an empty response.");
            
            Move botMove = ParseMoveString(botResponse, Color.Black);
            
            Assert.True(position.IsLegal(botMove), $"Bot returned an illegal move: {botResponse}");
        }
        [Fact]
        public void GetMove_SendsZeroMoves_ReturnsValidMove()
        {
            Assert.True(File.Exists(RealBotPath), "Bot not found.");
            
            var player = new BotPlayer(RealBotPath);
            var position = new Position(); 
            var moves = new List<Move>(); 
            
            string botResponse = player.GetMove(moves);
            
            Assert.False(string.IsNullOrEmpty(botResponse), "Bot returned an empty response.");
            
            Move botMove = ParseMoveString(botResponse, Color.White);
            
            Assert.True(position.IsLegal(botMove), $"Bot returned an illegal first move: {botResponse}");
        }

        private Move ParseMoveString(string moveString, Color sideToMove)
        {
            if (string.IsNullOrEmpty(moveString) || moveString.Length < 2)
            {
                throw new FormatException($"Invalid move string format: {moveString}");
            }
            
            char pieceChar = moveString[0];
            
            
            string squareString = moveString.Substring(1).ToUpper();
            if (!Enum.TryParse<Square>(squareString, out Square square))
            {
                throw new FormatException($"Invalid square in move: {squareString}");
            }
            
            Piece piece;
            if (sideToMove == Color.White)
            {
                piece = pieceChar switch
                {
                    'P' => Piece.WhitePawn,
                    'N' => Piece.WhiteKnight,
                    'B' => Piece.WhiteBishop,
                    'R' => Piece.WhiteRook,
                    'Q' => Piece.WhiteQueen,
                    'K' => Piece.WhiteKing,
                    _ => throw new FormatException($"Invalid piece type in move: {pieceChar}")
                };
            }
            else 
            {
                piece = pieceChar switch
                {
                    'P' => Piece.BlackPawn,
                    'N' => Piece.BlackKnight,
                    'B' => Piece.BlackBishop,
                    'R' => Piece.BlackRook,
                    'Q' => Piece.BlackQueen,
                    'K' => Piece.BlackKing,
                    _ => throw new FormatException($"Invalid piece type in move: {pieceChar}")
                };
            }
            return new Move(piece, square);
        }
    }
}
