using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Bogar.UI.Chess;
using Bogar.BLL.Core;
using BllColor = Bogar.BLL.Core.Color;
using BllPieceType = Bogar.BLL.Core.PieceType;
using BllPiece = Bogar.BLL.Core.Piece;
using BllSquare = Bogar.BLL.Core.Square;
using BllMove = Bogar.BLL.Core.Move;

namespace Bogar.UI
{
    public partial class MatchWindow : Window
    {
        private Chess.Board chessBoard = new Chess.Board();
        private GameManager gameManager = new GameManager();
        private readonly Dictionary<(BllColor, BllPieceType), FrameworkElement> _pieceSourceElements = new();
        private readonly Duration _moveAnimationDuration = new Duration(TimeSpan.FromMilliseconds(450));

        private ObservableCollection<string> leftMoves = new ObservableCollection<string>();
        private ObservableCollection<string> rightMoves = new ObservableCollection<string>();
        private const int MaxSideMoves = 16;

        private readonly string whiteBotPath = "";
        private readonly string blackBotPath = "";

        public MatchWindow(string whiteBotPath, string blackBotPath)
        {
            InitializeComponent();

            this.whiteBotPath = whiteBotPath;
            this.blackBotPath = blackBotPath;
            gameManager.MoveDelayMilliseconds = 650;

            LeftMovesList.ItemsSource = leftMoves;
            RightMovesList.ItemsSource = rightMoves;
            ConfigurePieceSources();

            SetupGameManager();
            GenerateBoard();
            RenderBoard();
            UpdatePieceCounters();

            WhiteBotNameTextBlock.Text = string.IsNullOrEmpty(whiteBotPath)
                ? "No Bot"
                : System.IO.Path.GetFileNameWithoutExtension(whiteBotPath);

            BlackBotNameTextBlock.Text = string.IsNullOrEmpty(blackBotPath)
                ? "No Bot"
                : System.IO.Path.GetFileNameWithoutExtension(blackBotPath);

            StartMatchButton.Visibility = Visibility.Visible;
            StopMatchButton.Visibility = Visibility.Visible;
        }

        private void SetupGameManager()
        {
            gameManager.MoveExecuted += OnMoveExecuted;
            gameManager.ScoreUpdated += OnScoreUpdated;
            gameManager.GameStatusChanged += OnGameStatusChanged;
            gameManager.GameEnded += OnGameEnded;
            gameManager.TimerTick += OnTimerTick;
        }

        private void ConfigurePieceSources()
        {
            _pieceSourceElements[(BllColor.Black, BllPieceType.Pawn)] = BlackPawnSource;
            _pieceSourceElements[(BllColor.Black, BllPieceType.Knight)] = BlackKnightSource;
            _pieceSourceElements[(BllColor.Black, BllPieceType.Bishop)] = BlackBishopSource;
            _pieceSourceElements[(BllColor.Black, BllPieceType.Rook)] = BlackRookSource;
            _pieceSourceElements[(BllColor.Black, BllPieceType.Queen)] = BlackQueenSource;
            _pieceSourceElements[(BllColor.Black, BllPieceType.King)] = BlackKingSource;

            _pieceSourceElements[(BllColor.White, BllPieceType.Pawn)] = WhitePawnSource;
            _pieceSourceElements[(BllColor.White, BllPieceType.Knight)] = WhiteKnightSource;
            _pieceSourceElements[(BllColor.White, BllPieceType.Bishop)] = WhiteBishopSource;
            _pieceSourceElements[(BllColor.White, BllPieceType.Rook)] = WhiteRookSource;
            _pieceSourceElements[(BllColor.White, BllPieceType.Queen)] = WhiteQueenSource;
            _pieceSourceElements[(BllColor.White, BllPieceType.King)] = WhiteKingSource;
        }

        private void OnTimerTick(TimeSpan elapsed)
        {
            GameTimerText.Text = elapsed.ToString("mm':'ss'.'fff");
        }

        private async void OnMoveExecuted(BllMove move, BllColor playerColor)
        {
            if (!Dispatcher.CheckAccess())
            {
                await Dispatcher.InvokeAsync(() => OnMoveExecuted(move, playerColor));
                return;
            }

            string moveString = $"{GetPieceChar(move.Piece)}{SquareExtensions.ToAlgebraic(move.Square).ToUpper()}";

            var targetList = playerColor == BllColor.White ? rightMoves : leftMoves;
            if (targetList.Count >= MaxSideMoves)
                targetList.RemoveAt(0);
            targetList.Add(moveString);

            switch (playerColor) {
                case BllColor.White:
                    TurnInfoText.Text = "W";
                    TurnArrow.Text = "←";
                    break;
                case BllColor.Black:
                    TurnInfoText.Text = "B";
                    TurnArrow.Text = "→";
                    break;
                default:
                    break;
            }

            var animatedImage = await AnimatePieceArrivalAsync(move, playerColor);
            SyncBoardWithGameState();
            UpdatePieceCounters();
            UpdateScoreDisplay();

            if (animatedImage != null)
            {
                AnimationCanvas.Children.Remove(animatedImage);
            }
        }

        private void OnScoreUpdated(int score)
        {
            Dispatcher.Invoke(() =>
            {
                Title = $"Bogar - Score: {score}";
            });
        }

        private void OnGameStatusChanged(string status)
        {
            Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Game Status: {status}");
            });
        }

        private async Task<Image?> AnimatePieceArrivalAsync(BllMove move, BllColor moverColor)
        {
            if (AnimationCanvas == null || !_pieceSourceElements.TryGetValue((moverColor, PieceExtensions.TypeOfPiece(move.Piece)), out var sourceElement))
                return null;

            var targetBorder = GetBorderForSquare(move.Square);
            if (sourceElement == null || targetBorder == null)
                return null;

            var start = GetElementCenterOnCanvas(sourceElement);
            var end = GetElementCenterOnCanvas(targetBorder);
            if (!start.HasValue || !end.HasValue)
                return null;

            var resourceKey = GetPieceResourceKey(moverColor, PieceExtensions.TypeOfPiece(move.Piece));
            if (TryFindResource(resourceKey) is not ImageSource imageSource)
                return null;

            var animatedImage = new Image
            {
                Source = imageSource,
                Width = 40,
                Height = 40,
                RenderTransform = new TranslateTransform()
            };

            AnimationCanvas.Children.Add(animatedImage);

            double startLeft = start.Value.X - animatedImage.Width / 2;
            double startTop = start.Value.Y - animatedImage.Height / 2;
            double endLeft = end.Value.X - animatedImage.Width / 2;
            double endTop = end.Value.Y - animatedImage.Height / 2;

            Canvas.SetLeft(animatedImage, startLeft);
            Canvas.SetTop(animatedImage, startTop);

            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };
            var leftAnimation = new DoubleAnimation(startLeft, endLeft, _moveAnimationDuration) { EasingFunction = easing };
            var topAnimation = new DoubleAnimation(startTop, endTop, _moveAnimationDuration) { EasingFunction = easing };

            var tcs = new TaskCompletionSource();
            leftAnimation.Completed += (s, e) => tcs.TrySetResult();

            animatedImage.BeginAnimation(Canvas.LeftProperty, leftAnimation);
            animatedImage.BeginAnimation(Canvas.TopProperty, topAnimation);

            await tcs.Task;
            await Task.Delay(100);
            return animatedImage;
        }

        private Point? GetElementCenterOnCanvas(FrameworkElement element)
        {
            if (AnimationCanvas == null || !element.IsLoaded)
                return null;

            try
            {
                var transform = element.TransformToVisual(AnimationCanvas);
                var point = transform.Transform(new Point(0, 0));

                if (element is Panel panel)
                {
                    point.X += panel.ActualWidth / 2;
                    point.Y += panel.ActualHeight / 2;
                }
                else
                {
                    point.X += element.ActualWidth / 2;
                    point.Y += element.ActualHeight / 2;
                }

                return point;
            }
            catch
            {
                return null;
            }
        }

        private Border? GetBorderForSquare(BllSquare square)
        {
            int col = square.GetFile();
            int row = 7 - square.GetRank();

            foreach (var child in ChessUniformGrid.Children)
            {
                if (child is Border border && border.Tag is BoardCellInfo info && info.Row == row && info.Col == col)
                    return border;
            }

            return null;
        }

        private string GetPieceResourceKey(BllColor color, BllPieceType type)
        {
            string prefix = color == BllColor.White ? "w" : "b";
            string suffix = type switch
            {
                BllPieceType.Pawn => "P",
                BllPieceType.Knight => "N",
                BllPieceType.Bishop => "B",
                BllPieceType.Rook => "R",
                BllPieceType.Queen => "Q",
                BllPieceType.King => "K",
                _ => "P"
            };
            return $"{prefix}{suffix}";
        }

        private void OnGameEnded(BllColor? winner)
        {
            Dispatcher.Invoke(() =>
            {
                StartMatchButton.IsEnabled = true;
                StopMatchButton.IsEnabled = false;

                WinnerTextBlock.Text = winner switch
                {
                    BllColor.White => "White wins!",
                    BllColor.Black => "Black wins!",
                    _ => "Game over"
                };
            });
        }

        private void SyncBoardWithGameState()
        {
            var position = gameManager.GetCurrentPosition();
            if (position == null) return;

            chessBoard = new Chess.Board();

            for (BllSquare sq = BllSquare.A1; sq < BllSquare.SQ_COUNT; sq++)
            {
                var piece = position.GetBoard().PieceAt(sq);
                if (piece != BllPiece.NoPiece)
                {
                    var uiPieceType = ConvertToUIPieceType(PieceExtensions.TypeOfPiece(piece));
                    var uiPieceColor = PieceExtensions.ColorOfPiece(piece) == BllColor.White ? PieceColor.White : PieceColor.Black;

                    string algebraic = $"{GetPieceChar(piece)}{SquareExtensions.ToAlgebraic(sq).ToUpper()}";
                    chessBoard.PlaceBySan(uiPieceColor, algebraic, out _);
                }
            }

            RenderBoard();
        }

        private char GetPieceChar(BllPiece piece)
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

        private Chess.PieceType ConvertToUIPieceType(BllPieceType pieceType)
        {
            return pieceType switch
            {
                BllPieceType.Pawn => Chess.PieceType.Pawn,
                BllPieceType.Knight => Chess.PieceType.Knight,
                BllPieceType.Bishop => Chess.PieceType.Bishop,
                BllPieceType.Rook => Chess.PieceType.Rook,
                BllPieceType.Queen => Chess.PieceType.Queen,
                BllPieceType.King => Chess.PieceType.King,
                _ => Chess.PieceType.Pawn
            };
        }

        private void StartMatchButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(blackBotPath))
            {
                MessageBox.Show("Please select a black bot first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            leftMoves.Clear();
            rightMoves.Clear();

            chessBoard = new Chess.Board();
            RenderBoard();
            UpdatePieceCounters();

            TurnInfoText.Text = "0";
            GameTimerText.Text = "00:00.000";
            WinnerTextBlock.Text = string.Empty;

            gameManager.StartGame(whiteBotPath, blackBotPath);

            StartMatchButton.IsEnabled = false;
            StopMatchButton.IsEnabled = true;
        }

        private void StopMatchButton_Click(object sender, RoutedEventArgs e)
        {
            gameManager.StopGame();

            StartMatchButton.IsEnabled = true;
            StopMatchButton.IsEnabled = false;
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

                    var info = (BoardCellInfo)border.Tag!;
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

                        var grid = new Grid();
                        grid.Children.Add(img);
                        border.Child = grid;
                    }
                }
            }
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
            int score = gameManager.GetCurrentScore();
            ScoreText.Text = $"Score: {score}";
        }

        private void LeaveMatchButton_Click(object sender, RoutedEventArgs e)
        {
            gameManager.StopGame();
            var startWindow = new StartWindow();
            WindowNavigationHelper.Replace(this, startWindow);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }

    public class BoardCellInfo
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }
}
