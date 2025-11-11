using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Bogar.UI.Chess;
using Bogar.BLL.Core;
using BllColor = Bogar.BLL.Core.Color;
using BllPieceType = Bogar.BLL.Core.PieceType;
using BllPiece = Bogar.BLL.Core.Piece;
using BllSquare = Bogar.BLL.Core.Square;
using BllMove = Bogar.BLL.Core.Move;

namespace Bogar.UI
{
    public partial class MainWindow : Window
    {
        private Chess.Board chessBoard = new Chess.Board();
        private GameManager gameManager = new GameManager();

        private ObservableCollection<string> leftMoves = new ObservableCollection<string>();
        private ObservableCollection<string> rightMoves = new ObservableCollection<string>();
        private const int MaxSideMoves = 16;

        private string whiteBotPath = "";
        private string blackBotPath = "";

        public MainWindow(string whiteBotPath, string blackBotPath)
        {
            InitializeComponent();

            this.whiteBotPath = whiteBotPath;
            this.blackBotPath = blackBotPath;

            LeftMovesList.ItemsSource = leftMoves;
            RightMovesList.ItemsSource = rightMoves;

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

        private void OnTimerTick(TimeSpan elapsed)
        {
            GameTimerText.Text = elapsed.ToString(@"mm\:ss\:ff");
        }

        private void OnMoveExecuted(BllMove move, BllColor playerColor)
        {
            Dispatcher.Invoke(() =>
            {
                string moveString = $"{GetPieceChar(move.Piece)}{SquareExtensions.ToAlgebraic(move.Square).ToUpper()}";
                
                var targetList = playerColor == BllColor.White ? rightMoves : leftMoves;
                if (targetList.Count >= MaxSideMoves)
                    targetList.RemoveAt(0);
                targetList.Add(moveString);

                SyncBoardWithGameState();
                UpdatePieceCounters();
                UpdateScoreDisplay();
            });
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

        private void OnGameEnded()
        {
            Dispatcher.Invoke(() =>
            {
                StartMatchButton.IsEnabled = true;
                StopMatchButton.IsEnabled = false;
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
            GameTimerText.Text = "00:00:00";
            
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

        private void ExitToMenu_Click(object sender, RoutedEventArgs e)
        {
            gameManager.StopGame();
            
            var startWindow = new StartWindow();
            startWindow.Show();
            this.Close();
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
            TurnInfoText.Text = score.ToString();
        }
    }

    public class BoardCellInfo
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }
}
