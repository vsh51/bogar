using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Bogar.BLL.Core;
using Bogar.BLL.Networking;
using Bogar.UI.Chess;
using BllPiece = Bogar.BLL.Core.Piece;
using BllPieceType = Bogar.BLL.Core.PieceType;
using BllSquare = Bogar.BLL.Core.Square;
using BllColor = Bogar.BLL.Core.Color;

namespace Bogar.UI
{
    public partial class AdminMatchWindow : Window
    {
        private readonly GameServer _server;
        private readonly Guid _whiteId;
        private readonly Guid _blackId;
        private readonly string _whiteName;
        private readonly string _blackName;
        private readonly DispatcherTimer _timer;
        private DateTime? _matchStart;

        private Chess.Board chessBoard = new Chess.Board();
        private Position _position = new Position();

        public AdminMatchWindow(GameServer server, Guid whiteId, Guid blackId, string whiteName, string blackName)
        {
            InitializeComponent();

            _server = server;
            _whiteId = whiteId;
            _blackId = blackId;
            _whiteName = whiteName;
            _blackName = blackName;

            WhiteBotNameTextBlock.Text = whiteName;
            BlackBotNameTextBlock.Text = blackName;

            GenerateBoard();
            RenderBoard();
            UpdatePieceCounters();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += (_, _) =>
            {
                if (_matchStart.HasValue)
                {
                    GameTimerText.Text = (DateTime.Now - _matchStart.Value).ToString(@"mm\:ss");
                }
            };

            _server.GameStarted += OnGameStarted;
            _server.MoveExecuted += OnMoveExecuted;
            _server.GameEnded += OnGameEnded;
        }

        private bool MatchesPair(ConnectedClient white, ConnectedClient black)
            => white.Id == _whiteId && black.Id == _blackId;

        private void OnGameStarted(ConnectedClient white, ConnectedClient black)
        {
            if (!MatchesPair(white, black))
                return;

            Dispatcher.Invoke(() =>
            {
                ResetBoard();
                _matchStart = DateTime.Now;
                _timer.Start();
            });
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

                SyncBoardWithPosition();
                UpdatePieceCounters();
                UpdateScoreDisplay();
            });
        }

        private void OnGameEnded(ConnectedClient white, ConnectedClient black, BllColor? winner)
        {
            if (!MatchesPair(white, black))
                return;

            Dispatcher.Invoke(() =>
            {
                _timer.Stop();
                _matchStart = null;
                GameTimerText.Text = winner switch
                {
                    BllColor.White => $"{_whiteName} wins",
                    BllColor.Black => $"{_blackName} wins",
                    _ => "Draw"
                };
            });
        }

        private void ResetBoard()
        {
            _position = new Position();
            chessBoard = new Chess.Board();
            TurnInfoText.Text = "0";
            GameTimerText.Text = "00:00";
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

        private void LeaveMatch_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void StartMatchButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void StartNextMatch_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ViewStatistics_Click(object sender, RoutedEventArgs e)
        {
        }

        private void LeaveLobby_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void StopMatch_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ResumeMatch_Click(object sender, RoutedEventArgs e)
        {
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _server.GameStarted -= OnGameStarted;
            _server.MoveExecuted -= OnMoveExecuted;
            _server.GameEnded -= OnGameEnded;
            _timer.Stop();
        }
    }

}
