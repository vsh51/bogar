using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Bogar.BLL.Game;
using Bogar.BLL.Player;
using Bogar.BLL.Core;

namespace Bogar.UI
{
    public class GameManager
    {
        private Game? _currentGame;
        private bool _isGameRunning;
        private bool _isMoveInProgress;
        private DateTime _gameStartTime;
        private DateTime _moveStartTime;

        private readonly DispatcherTimer _uiTimer;

        private const int MoveTimeLimitSeconds = 20;

        public event Action<Move, Color>? MoveExecuted;
        public event Action<int>? ScoreUpdated;
        public event Action<string>? GameStatusChanged;
        public event Action<Color?>? GameEnded;
        public event Action<TimeSpan>? TimerTick;

        public bool IsGameActive =>
            _currentGame != null && !_currentGame.IsGameOver() && _isGameRunning;

        public int MoveDelayMilliseconds { get; set; }

        public GameManager()
        {
            _uiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(13)
            };

            _uiTimer.Tick += (s, e) =>
            {
                if (!_isGameRunning)
                    return;

                var moveElapsed = DateTime.Now - _moveStartTime;
                var gameElapsed = DateTime.Now - _gameStartTime;

                TimerTick?.Invoke(gameElapsed);

                if (_isMoveInProgress && moveElapsed.TotalSeconds > MoveTimeLimitSeconds)
                {
                    HandleMoveTimeout();
                }
            };
        }

        public void StartGame(string whiteBotPath, string blackBotPath)
        {
            try
            {
                var whitePlayer = new Player(whiteBotPath);
                var blackPlayer = new Player(blackBotPath);

                _currentGame = new Game(whitePlayer, blackPlayer);

                _isGameRunning = true;
                _isMoveInProgress = false;
                _gameStartTime = DateTime.Now;
                _moveStartTime = _gameStartTime;

                _uiTimer.Start();

                GameStatusChanged?.Invoke("Game started");

                _ = RunGameLoopAsync();
            }
            catch (Exception ex)
            {
                GameStatusChanged?.Invoke($"Failed to start game: {ex.Message}");
            }
        }

        public void StopGame()
        {
            _isGameRunning = false;
            _isMoveInProgress = false;
            _uiTimer.Stop();
            _currentGame = null;

            GameStatusChanged?.Invoke("Game stopped");
        }

        private async Task RunGameLoopAsync()
        {
            while (_isGameRunning && _currentGame != null && !_currentGame.IsGameOver())
            {
                if (!_isMoveInProgress)
                {
                    _isMoveInProgress = true;
                    await ExecuteNextMoveAsync();
                    _isMoveInProgress = false;
                }

                await Task.Delay(10);
            }
        }

        public async Task ExecuteNextMoveAsync()
        {
            var game = _currentGame;
            if (game == null || !IsGameActive)
                return;

            _moveStartTime = DateTime.Now;

            var moveColor = game.GetCurrentTurn();

            try
            {
                await Task.Run(() => game.DoNextMove());

                if (!_isGameRunning || game != _currentGame)
                    return;

                var lastMove = game.Moves[^1];
                var score = game.GetScore();

                DispatchUI(() =>
                {
                    MoveExecuted?.Invoke(lastMove, moveColor);
                    ScoreUpdated?.Invoke(score);
                });

                if (MoveDelayMilliseconds > 0)
                {
                    await Task.Delay(MoveDelayMilliseconds);
                }

                if (game.IsGameOver())
                {
                    var winner = DetermineWinner(game);
                    var statusMessage = winner switch
                    {
                        Color.White => "Game completed — White wins",
                        Color.Black => "Game completed — Black wins",
                        _ => "Game completed — Draw"
                    };

                    EndGame(statusMessage, winner);
                }
            }
            catch (Exception ex)
            {
                EndGame($"Move error: {ex.Message}");
            }
        }

        private void HandleMoveTimeout()
        {
            if (_currentGame == null || !_isGameRunning)
                return;

            var game = _currentGame;

            var loser = game.GetCurrentTurn();

            _isGameRunning = false;
            _isMoveInProgress = false;
            _uiTimer.Stop();

            DispatchUI(() =>
            {
                var winner = GetOppositeColor(loser);
                GameEnded?.Invoke(winner);
                GameStatusChanged?.Invoke(
                    $"Ліміт у {MoveTimeLimitSeconds} секунд вичерпано. " +
                    $"Хід гравця {loser} не виконано — переміг інший гравець."
                );
            });
        }

        private void EndGame(string message, Color? winner = null)
        {
            if (!_isGameRunning)
                return;

            _isGameRunning = false;
            _isMoveInProgress = false;
            _uiTimer.Stop();

            DispatchUI(() =>
            {
                GameEnded?.Invoke(winner);
                GameStatusChanged?.Invoke(message);
            });
        }

        private static Color GetOppositeColor(Color color) =>
            color == Color.White ? Color.Black : Color.White;

        private Color? DetermineWinner(Game game)
        {
            var score = game.GetScore();

            if (score > 0)
                return Color.White;
            if (score < 0)
                return Color.Black;

            return null;
        }

        private void DispatchUI(Action action)
        {
            App.Current.Dispatcher.Invoke(action);
        }

        public Position? GetCurrentPosition() => _currentGame?.GetCurrentPosition();

        public int GetCurrentScore() => _currentGame?.GetScore() ?? 0;

        public TimeSpan GetElapsedTime() =>
            _isGameRunning ? DateTime.Now - _gameStartTime : TimeSpan.Zero;
    }
}
