using System;
using System.Threading.Tasks;
using Bogar.BLL.Game;
using Bogar.BLL.Player;
using Bogar.BLL.Core;

namespace Bogar.UI
{
    public class GameManager
    {
        private Game? _currentGame;
        private bool _isGameRunning;

        public event Action<Move, Color>? MoveExecuted;
        public event Action<int>? ScoreUpdated;
        public event Action<string>? GameStatusChanged;
        public event Action? GameEnded;

        public bool IsGameActive => _currentGame != null && !_currentGame.IsGameOver();

        public void StartGame(string whiteBotPath, string blackBotPath)
        {
            try
            {
                var whitePlayer = new Player(whiteBotPath);
                var blackPlayer = new Player(blackBotPath);
                
                _currentGame = new Game(whitePlayer, blackPlayer);
                _isGameRunning = true;
                
                GameStatusChanged?.Invoke("Game started");
            }
            catch (Exception ex)
            {
                GameStatusChanged?.Invoke($"Failed to start game: {ex.Message}");
            }
        }

        public void StopGame()
        {
            _isGameRunning = false;
            _currentGame = null;
            GameStatusChanged?.Invoke("Game stopped");
        }

        public async Task ExecuteNextMoveAsync()
        {
            var game = _currentGame;
            if (game == null || !_isGameRunning || game.IsGameOver())
                return;

            try
            {
                await Task.Run(() =>
                {
                    var currentTurn = game.GetCurrentTurn();
                    game.DoNextMove();
                    
                    var lastMove = game.Moves[^1];
                    var score = game.GetScore();
                    
                    MoveExecuted?.Invoke(lastMove, currentTurn);
                    ScoreUpdated?.Invoke(score);
                    
                    if (game.IsGameOver())
                    {
                        _isGameRunning = false;
                        GameEnded?.Invoke();
                        GameStatusChanged?.Invoke("Game completed");
                    }
                });
            }
            catch (Exception ex)
            {
                GameStatusChanged?.Invoke($"Move error: {ex.Message}");
                _isGameRunning = false;
            }
        }

        public Position? GetCurrentPosition()
        {
            return _currentGame?.GetCurrentPosition();
        }

        public int GetCurrentScore()
        {
            return _currentGame?.GetScore() ?? 0;
        }
    }
}