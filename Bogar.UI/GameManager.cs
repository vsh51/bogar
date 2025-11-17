using System;
using System.Threading;
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

        private readonly DispatcherTimer _uiTimer;

        private const int MoveTimeLimitSeconds = 20;

        public event Action<Move, Color>? MoveExecuted;
        public event Action<int>? ScoreUpdated;
        public event Action<string>? GameStatusChanged;
        public event Action? GameEnded;
        public event Action<TimeSpan>? TimerTick;

        public bool IsGameActive => _currentGame != null && !_currentGame.IsGameOver();

        public GameManager()
        {
            _uiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(13)
            };

            _uiTimer.Tick += (s, e) =>
            {
                if (_isGameRunning)
                {
                    var elapsed = DateTime.Now - _gameStartTime;
                    TimerTick?.Invoke(elapsed);
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

            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(MoveTimeLimitSeconds)
            );

            try
            {
                await Task.Run(() =>
                {
                    game.DoNextMove();
                }, cts.Token);

                var lastMove = game.Moves[^1];
                var score = game.GetScore();
                var currentTurn = game.GetCurrentTurn();

                DispatchUI(() =>
                {
                    MoveExecuted?.Invoke(lastMove, currentTurn);
                    ScoreUpdated?.Invoke(score);
                });

                if (game.IsGameOver())
                {
                    EndGame("Game completed");
                }
            }
            catch (OperationCanceledException)
            {
                EndGame("Move timeout — game ended");
            }
            catch (Exception ex)
            {
                EndGame($"Move error: {ex.Message}");
            }
        }

        private void EndGame(string message)
        {
            if (!_isGameRunning) return;

            _isGameRunning = false;
            _uiTimer.Stop();

            DispatchUI(() =>
            {
                GameEnded?.Invoke();
                GameStatusChanged?.Invoke(message);
            });
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
