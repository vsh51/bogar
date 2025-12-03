using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Bogar.BLL.Core;
using Bogar.BLL.Networking;
using Bogar.BLL.Statistics;
using Bogar.UI.Chess;
using BllPiece = Bogar.BLL.Core.Piece;
using BllPieceType = Bogar.BLL.Core.PieceType;
using BllSquare = Bogar.BLL.Core.Square;
using BllColor = Bogar.BLL.Core.Color;
using Serilog;

namespace Bogar.UI
{
    public partial class AdminMatchWindow : Window
    {
        private readonly GameServer _server;
        private readonly string _lobbyName;
        private readonly LobbyStatisticsService _statisticsService;
        private Guid _whiteId;
        private Guid _blackId;
        private string _whiteName = string.Empty;
        private string _blackName = string.Empty;
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _waitingListTimer;
        private DateTime? _matchStart;
        private bool _isMatchRunning;
        private bool _isMatchPaused;
        private TimeSpan _elapsedBeforePause = TimeSpan.Zero;

        private readonly ObservableCollection<string> _leftMoves = new();
        private readonly ObservableCollection<string> _rightMoves = new();
        private readonly ObservableCollection<ClientListItem> _waitingClients = new();
        private bool _isRefreshingWaitingPlayers;
        private readonly List<Guid> _selectionOrder = new();

        private readonly AdminWaitingRoomWindow _waitingRoomWindow;

        private const int MaxSideMoves = 16;

        private Chess.Board chessBoard = new Chess.Board();
        private Position _position = new Position();

        public static readonly RoutedUICommand KickPlayerCommand = new RoutedUICommand(
            "Kick Player",
            "KickPlayer",
            typeof(AdminMatchWindow));

        public AdminMatchWindow(
            GameServer server,
            Guid whiteId,
            Guid blackId,
            string whiteName,
            string blackName,
            string lobbyName,
            string hostIp,
            LobbyStatisticsService statisticsService,
            AdminWaitingRoomWindow waitingRoomWindow
        )
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(KickPlayerCommand, OnKickPlayerCommandExecuted, CanExecuteKickPlayerCommand));

            _server = server;
            _lobbyName = lobbyName;
            _statisticsService = statisticsService;

            LeftMovesList.ItemsSource = _leftMoves;
            RightMovesList.ItemsSource = _rightMoves;
            WaitingPlayersList.ItemsSource = _waitingClients;
            _waitingRoomWindow = waitingRoomWindow;

            LobbyNameText.Text = $"Lobby: {lobbyName}";
            LobbyIpText.Text = $"IP: {hostIp}";

            GenerateBoard();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += (_, _) =>
            {
                if (_matchStart.HasValue)
                {
                    var elapsed = _elapsedBeforePause + (DateTime.Now - _matchStart.Value);
                    GameTimerText.Text = elapsed.ToString("mm':'ss'.'fff");
                }
            };

            ResetBoard();

            _waitingListTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _waitingListTimer.Tick += (_, _) => RefreshWaitingPlayers();

            SetCurrentMatch(whiteId, blackId, whiteName, blackName);

            _waitingListTimer.Start();
            RefreshWaitingPlayers();

            _server.GameStarted += OnGameStarted;
            _server.MoveExecuted += OnMoveExecuted;
            _server.GameEnded += OnGameEnded;
            Log.Information("Admin match window opened for lobby {Lobby}. Current match {White} vs {Black}", _lobbyName, whiteName, blackName);
        }

        private bool MatchesPair(ConnectedClient white, ConnectedClient black)
            => _whiteId != Guid.Empty && _blackId != Guid.Empty &&
               white.Id == _whiteId && black.Id == _blackId;

        private void SetCurrentMatch(Guid whiteId, Guid blackId, string whiteName, string blackName)
        {
            _whiteId = whiteId;
            _blackId = blackId;
            _whiteName = whiteName;
            _blackName = blackName;

            WhiteBotNameTextBlock.Text = whiteName;
            BlackBotNameTextBlock.Text = blackName;

            SetMatchState(false, false);
            Log.Information("Tracking current match White={White} Black={Black}", whiteName, blackName);
        }

        private void SetMatchState(bool isRunning, bool isPaused)
        {
            _isMatchRunning = isRunning;
            _isMatchPaused = isPaused;

            StopMatchButton.IsEnabled = isRunning && !isPaused;
            ResumeMatchButton.IsEnabled = isRunning && isPaused;

            UpdateStartNextMatchButtonState();
        }

        private void OnGameStarted(ConnectedClient white, ConnectedClient black)
        {
            if (!MatchesPair(white, black))
                return;

            Dispatcher.Invoke(() =>
            {
                SetMatchState(true, false);
                ResetBoard();
                _elapsedBeforePause = TimeSpan.Zero;
                _matchStart = DateTime.Now;
                _timer.Start();
            });
            Log.Information("Observed game start for {White} vs {Black}", white.Nickname, black.Nickname);
        }

        private void OnMoveExecuted(ConnectedClient white, ConnectedClient black, Move move, BllColor moveColor)
        {
            if (!MatchesPair(white, black))
                return;

            Dispatcher.Invoke(() =>
            {
                try
                {
                    _position.DoMove(move);
                }
                catch
                {
                    // ignore visual sync issues
                }

                switch (moveColor)
                {
                    case BllColor.White:
                        TurnArrow.Text = "←";
                        break;
                    case BllColor.Black:
                        TurnArrow.Text = "→";
                        break;
                    default:
                        break;
                }

                SyncBoardWithPosition();
                UpdatePieceCounters();
                UpdateScoreDisplay();
                AddMoveToList(move, moveColor);
            });
        }

        private void OnGameEnded(ConnectedClient white, ConnectedClient black, BllColor? winner)
        {
            if (!MatchesPair(white, black))
                return;

            Dispatcher.Invoke(() =>
            {
                if (_matchStart.HasValue)
                {
                    _elapsedBeforePause += DateTime.Now - _matchStart.Value;
                }

                _timer.Stop();
                _matchStart = null;
                GameTimerText.Text = _elapsedBeforePause.ToString("mm':'ss'.'fff");
                WinnerTextBlock.Text = winner switch
                {
                    BllColor.White => $"{_whiteName} wins",
                    BllColor.Black => $"{_blackName} wins",
                    _ => "Draw"
                };

                SetMatchState(false, false);
                RefreshWaitingPlayers();
            });
            Log.Information("Observed game end for {White} vs {Black}", white.Nickname, black.Nickname);
        }

        private void ResetBoard()
        {
            _position = new Position();
            chessBoard = new Chess.Board();
            TurnInfoText.Text = "0";
            GameTimerText.Text = "00:00.000";
            WinnerTextBlock.Text = string.Empty;
            _leftMoves.Clear();
            _rightMoves.Clear();
            _matchStart = null;
            _elapsedBeforePause = TimeSpan.Zero;
            _timer.Stop();
            RenderBoard();
            UpdatePieceCounters();
            UpdateScoreDisplay();
        }

        private void GenerateBoard()
        {
            ChessUniformGrid.Children.Clear();
            var light = (Brush)FindResource("LightSquareBrush");
            var dark = (Brush)FindResource("DarkSquareBrush");

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    bool isLight = (row + col) % 2 == 0;

                    var border = new Border
                    {
                        Background = isLight ? light : dark,
                        Tag = new BoardCellInfo { Row = row, Col = col }
                    };

                    ChessUniformGrid.Children.Add(border);
                }
            }
        }

        private void RenderBoard()
        {
            foreach (var child in ChessUniformGrid.Children)
            {
                if (child is Border border)
                {
                    border.Child = null;

                    if (border.Tag is BoardCellInfo info)
                    {
                        var piece = chessBoard.GetPieceAt(info.Row, info.Col);
                        if (piece != null)
                        {
                            var img = new Image
                            {
                                Source = piece.Image,
                                Width = 50,
                                Height = 50,
                                Stretch = Stretch.Uniform
                            };
                            border.Child = new Grid { Children = { img } };
                        }
                    }
                }
            }
        }

        private void SyncBoardWithPosition()
        {
            chessBoard = new Chess.Board();
            var boardState = _position.GetBoard();

            for (BllSquare sq = BllSquare.A1; sq < BllSquare.SQ_COUNT; sq++)
            {
                var piece = boardState.PieceAt(sq);
                if (piece == BllPiece.NoPiece)
                    continue;

                var color = PieceExtensions.ColorOfPiece(piece) == BllColor.White ? PieceColor.White : PieceColor.Black;
                var san = $"{GetPieceChar(piece)}{SquareExtensions.ToAlgebraic(sq).ToUpper()}";
                chessBoard.PlaceBySan(color, san, out _);
            }

            RenderBoard();
        }

        private void UpdatePieceCounters()
        {
            LeftPawnCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, Chess.PieceType.Pawn)}";
            LeftKnightCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, Chess.PieceType.Knight)}";
            LeftBishopCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, Chess.PieceType.Bishop)}";
            LeftRookCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, Chess.PieceType.Rook)}";
            LeftQueenCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, Chess.PieceType.Queen)}";
            LeftKingCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, Chess.PieceType.King)}";

            RightPawnCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, Chess.PieceType.Pawn)}";
            RightKnightCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, Chess.PieceType.Knight)}";
            RightBishopCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, Chess.PieceType.Bishop)}";
            RightRookCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, Chess.PieceType.Rook)}";
            RightQueenCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, Chess.PieceType.Queen)}";
            RightKingCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, Chess.PieceType.King)}";
        }

        private void UpdateScoreDisplay()
        {
            var (whiteScore, blackScore) = _position.CalculateScore();
            TurnInfoText.Text = (whiteScore - blackScore).ToString();
        }

        private void AddMoveToList(Move move, BllColor moveColor)
        {
            var moveString = $"{GetPieceChar(move.Piece)}{SquareExtensions.ToAlgebraic(move.Square).ToUpper()}";
            var targetList = moveColor == BllColor.White ? _rightMoves : _leftMoves;

            if (targetList.Count >= MaxSideMoves)
            {
                targetList.RemoveAt(0);
            }

            targetList.Add(moveString);
        }

        private void PauseVisualTimer()
        {
            if (_matchStart.HasValue)
            {
                _elapsedBeforePause += DateTime.Now - _matchStart.Value;
                _matchStart = null;
            }

            _timer.Stop();
            GameTimerText.Text = _elapsedBeforePause.ToString("mm':'ss'.'fff");
        }

        private void ResumeVisualTimer()
        {
            if (!_matchStart.HasValue)
            {
                _matchStart = DateTime.Now;
            }

            _timer.Start();
        }

        private void RefreshWaitingPlayers()
        {
            if (_isRefreshingWaitingPlayers)
                return;

            try
            {
                _isRefreshingWaitingPlayers = true;

                var previouslySelected = WaitingPlayersList.SelectedItems
                    .OfType<ClientListItem>()
                    .Select(item => item.ClientId)
                    .ToHashSet();

                var idleClients = _server.GetConnectedClients()
                    .Where(c => !_server.IsClientInGame(c.Id))
                    .Select(c => new ClientListItem(c.Id, c.Nickname))
                    .OrderBy(c => c.Nickname, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _waitingClients.Clear();
                foreach (var client in idleClients)
                {
                    _waitingClients.Add(client);
                }

                WaitingPlayersList.SelectedItems.Clear();
                foreach (var client in _waitingClients)
                {
                    if (previouslySelected.Contains(client.ClientId))
                    {
                        WaitingPlayersList.SelectedItems.Add(client);
                    }
                }

                WaitingPlayersEmptyText.Visibility = _waitingClients.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            finally
            {
                _isRefreshingWaitingPlayers = false;
                UpdateStartNextMatchButtonState();
            }
        }

        private List<ClientListItem> GetSelectedClientsInOrder()
        {
            var selected = WaitingPlayersList.SelectedItems
                .OfType<ClientListItem>()
                .ToList();

            if (selected.Count <= 1)
                return selected;

            var lookup = selected.ToDictionary(item => item.ClientId);
            var ordered = new List<ClientListItem>();

            foreach (var id in _selectionOrder)
            {
                if (lookup.TryGetValue(id, out var client))
                {
                    ordered.Add(client);
                    if (ordered.Count == selected.Count)
                        break;
                }
            }

            if (ordered.Count < selected.Count)
            {
                foreach (var client in selected)
                {
                    if (!ordered.Contains(client))
                    {
                        ordered.Add(client);
                    }
                }
            }

            return ordered;
        }

        private void UpdateStartNextMatchButtonState()
        {
            if (_isRefreshingWaitingPlayers)
                return;

            var selectedCount = WaitingPlayersList.SelectedItems
                .OfType<ClientListItem>()
                .Count();

            StartNextMatchButton.IsEnabled = !_isMatchRunning && selectedCount == 2;
        }

        private void WaitingPlayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isRefreshingWaitingPlayers)
                return;

            foreach (var removed in e.RemovedItems.OfType<ClientListItem>())
            {
                _selectionOrder.Remove(removed.ClientId);
            }

            foreach (var added in e.AddedItems.OfType<ClientListItem>())
            {
                if (!_selectionOrder.Contains(added.ClientId))
                {
                    _selectionOrder.Add(added.ClientId);
                }
            }

            var selectedIds = WaitingPlayersList.SelectedItems
                .OfType<ClientListItem>()
                .Select(c => c.ClientId)
                .ToHashSet();

            _selectionOrder.RemoveAll(id => !selectedIds.Contains(id));

            UpdateStartNextMatchButtonState();
        }

        private static char GetPieceChar(BllPiece piece)
        {
            return PieceExtensions.TypeOfPiece(piece) switch
            {
                BllPieceType.Pawn => 'P',
                BllPieceType.Knight => 'N',
                BllPieceType.Bishop => 'B',
                BllPieceType.Rook => 'R',
                BllPieceType.Queen => 'Q',
                BllPieceType.King => 'K',
                _ => '?'
            };
        }

        private void StartNextMatch_Click(object sender, RoutedEventArgs e)
        {
            if (_isMatchRunning)
                return;

            var selected = GetSelectedClientsInOrder();
            if (selected.Count != 2)
                return;

            if (selected[0].ClientId == selected[1].ClientId)
            {
                MessageBox.Show("Please select two different players.",
                    "Cannot start match", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var clients = _server.GetConnectedClients()
                .ToDictionary(c => c.Id, c => c);

            if (!clients.TryGetValue(selected[0].ClientId, out var white) ||
                !clients.TryGetValue(selected[1].ClientId, out var black))
            {
                MessageBox.Show("Selected players are no longer available.",
                    "Cannot start match", MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshWaitingPlayers();
                return;
            }

            if (_server.TryStartMatch(white.Id, black.Id, out var error))
            {
                SetCurrentMatch(white.Id, black.Id, white.Nickname, black.Nickname);
                WaitingPlayersList.SelectedItems.Clear();
                RefreshWaitingPlayers();
                Log.Information("Manual match started: {White} vs {Black}", white.Nickname, black.Nickname);
            }
            else if (!string.IsNullOrEmpty(error))
            {
                Log.Warning("Failed to start manual match {White} vs {Black}: {Error}", white.Nickname, black.Nickname, error);
                MessageBox.Show(error,
                    "Cannot start match", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                Log.Warning("Unknown error while starting manual match {White} vs {Black}", white.Nickname, black.Nickname);
                MessageBox.Show("Unable to start the selected match.",
                    "Cannot start match", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LeaveLobby_Click(object sender, RoutedEventArgs e)
        {
            _waitingRoomWindow.NavigateToStart();
            Log.Information("Admin leaving lobby from match view");
            this.Close();
        }

        private void StopMatch_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMatchRunning || _isMatchPaused)
                return;

            if (_server.TryPauseMatch(_whiteId, _blackId))
            {
                PauseVisualTimer();
                SetMatchState(true, true);
                Log.Information("Match paused: {White} vs {Black}", _whiteName, _blackName);
            }
            else
            {
                MessageBox.Show("Unable to pause the current match. It may have already finished.",
                    "Pause failed", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Warning("Pause failed for match {White} vs {Black}", _whiteName, _blackName);
            }
        }

        private void ResumeMatch_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMatchRunning || !_isMatchPaused)
                return;

            if (_server.TryResumeMatch(_whiteId, _blackId))
            {
                ResumeVisualTimer();
                SetMatchState(true, false);
                Log.Information("Match resumed: {White} vs {Black}", _whiteName, _blackName);
            }
            else
            {
                MessageBox.Show("Unable to resume the current match. It may have already finished.",
                    "Resume failed", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Warning("Resume failed for match {White} vs {Black}", _whiteName, _blackName);
            }
        }
        private void ViewStatistics_Click(object sender, RoutedEventArgs e)
        {
            var statisticsWindow = new AdminStatisticsWindow(_statisticsService, _lobbyName);
            Log.Information("Viewing statistics for lobby {Lobby}", _lobbyName);
            statisticsWindow.Show();
        }

        private void KickWhitePlayer_Click(object sender, RoutedEventArgs e)
        {
            KickPlayer(_whiteId, _whiteName);
        }

        private void KickBlackPlayer_Click(object sender, RoutedEventArgs e)
        {
            KickPlayer(_blackId, _blackName);
        }

        private void OnKickPlayerCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is ClientListItem item)
            {
                KickPlayer(item.ClientId, item.Nickname);
            }
        }

        private void CanExecuteKickPlayerCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is ClientListItem;
        }

        private void KickPlayer(Guid playerId, string playerName)
        {
            if (playerId == Guid.Empty)
                return;

            var confirm = MessageBox.Show(
                $"Kick player '{playerName}' from the lobby?",
                "Kick player",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            Log.Information("Attempting to kick player {Player}", playerName);
            if (_server.TryKickClient(playerId, out var error))
            {
                MessageBox.Show(
                    $"Player '{playerName}' was kicked.",
                    "Kick player",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Log.Information("Player kicked from match context: {Player}", playerName);
                RefreshWaitingPlayers();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "Kick player", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Warning("Failed to kick player {Player}: {Error}", playerName, error);
            }
            else
            {
                MessageBox.Show(
                    "Unable to kick the selected player.",
                    "Kick player",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Log.Warning("Unable to kick player {Player} due to unknown error", playerName);
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _waitingRoomWindow.NavigateToStart();
            Log.Information("Admin match window closed");
            this.Close();
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isMatchRunning)
            {
                _server.StopMatch(_whiteId, _blackId);
                Log.Information("Force stopping match {White} vs {Black} before leaving", _whiteName, _blackName);
            }
            _waitingRoomWindow.Show();
            Log.Information("Navigating back to waiting room window");
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _server.GameStarted -= OnGameStarted;
            _server.MoveExecuted -= OnMoveExecuted;
            _server.GameEnded -= OnGameEnded;
            _timer.Stop();
            _waitingListTimer.Stop();
            Log.Information("Admin match window cleaned up");
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private sealed class ClientListItem
        {
            public ClientListItem(Guid clientId, string nickname)
            {
                ClientId = clientId;
                Nickname = nickname;
            }

            public Guid ClientId { get; }
            public string Nickname { get; }
        }
    }

}
