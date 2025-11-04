using Xunit;
using System.IO;
using Bogar.BLL.Game;
using Bogar.BLL.Core;
using Bogar.BLL.Player;
using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using BotPlayer = Bogar.BLL.Player.Player;

namespace Bogar.BLL.Game
{
    public class GameTest
    {
        private const string WhiteBotPath = "/Users/work/Desktop/bots/a.out";
        private const string BlackBotPath = "/Users/work/Desktop/bots/a.out";

        private readonly ITestOutputHelper _output;

        public GameTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private void AssertBotsExist()
        {
            Assert.True(File.Exists(WhiteBotPath), $"Bot not found at {Path.GetFullPath(WhiteBotPath)}.");
            Assert.True(File.Exists(BlackBotPath), $"Bot not found at {Path.GetFullPath(BlackBotPath)}.");
        }

        [Fact]
        public void PlayFullGame_RunsUntilEnd_AndReportsWinner()
        {
            AssertBotsExist();

            var playerWhite = new BotPlayer(WhiteBotPath);
            var playerBlack = new BotPlayer(BlackBotPath);
            var game = new Game(playerWhite, playerBlack);

            Exception gameEndException = null;
            int maxMoves = 32;

            _output.WriteLine($"--- Початок гри ---");
            _output.WriteLine($"Білий бот: {WhiteBotPath}");
            _output.WriteLine($"Чорний бот: {BlackBotPath}");


            for (int i = 0; i < maxMoves; i++)
            {
                try
                {
                    game.DoNextMove();
                }
                catch (Exception ex)
                {
                    gameEndException = ex;
                    _output.WriteLine($"ПОМИЛКА: Гра зупинена на ході {i + 1} через помилку: {ex.Message}");
                    break;
                }
            }

            if (gameEndException == null)
            {
                _output.WriteLine($"УСПІХ: Всі 32 ходи зроблено легально.");

                Assert.Equal(maxMoves, game.Moves.Count);

                var exAfter32 = Assert.Throws<InvalidOperationException>(() => game.DoNextMove());

                Assert.Contains("Game is over", exAfter32.Message, StringComparison.OrdinalIgnoreCase);
                _output.WriteLine("Перевірено: 33-й хід неможливий (гра завершена).");
            }
            else
            {
                Assert.NotEqual(maxMoves, game.Moves.Count);
                _output.WriteLine($"ІНФО: Гра завершилася достроково на {game.Moves.Count} ході.");
                _output.WriteLine($"Причина: {gameEndException.Message}");
            }

            int finalScore = game.GetScore();
            string winner;
            if (finalScore > 0) winner = "Білі";
            else if (finalScore < 0) winner = "Чорні";
            else winner = "Нічия";
            _output.WriteLine($"--- ФІНАЛЬНИЙ РЕЗУЛЬТАТ ---");
            _output.WriteLine($"Всього ходів зроблено: {game.Moves.Count}");
            _output.WriteLine($"Рахунок (Білі - Чорні): {finalScore}");
            _output.WriteLine($"Переможець: {winner}");

            Assert.True(game.Moves.Count <= maxMoves);
        }
    }
}