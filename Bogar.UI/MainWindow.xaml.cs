using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32; 
using Bogar.UI.Chess;

namespace Bogar.UI
{
    public partial class MainWindow : Window
    {
        private Board chessBoard = new Board();
        private PieceColor currentTurn = PieceColor.White;

        private ObservableCollection<string> leftMoves = new ObservableCollection<string>();
        private ObservableCollection<string> rightMoves = new ObservableCollection<string>();
        private const int MaxSideMoves = 16;

        private string whiteBotPath = @"Bots\WhiteBot.exe";
        private string blackBotPath = "";

        public MainWindow()
        {
            InitializeComponent();

            LeftMovesList.ItemsSource = leftMoves;
            RightMovesList.ItemsSource = rightMoves;

            GenerateBoard();
            RenderBoard();
            UpdateTurnUI();
            UpdatePieceCounters();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && CommandTextBox.IsFocused)
            {
                SendCommand_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
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

        private void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            var cmd = CommandTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            if (chessBoard.PlaceBySan(currentTurn, cmd, out string error))
            {
                AddMoveToSide(currentTurn, cmd);
                RenderBoard();
                UpdatePieceCounters();
                UpdateTurnUI();
            }
            else
            {
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            CommandTextBox.Clear();
        }

        private void CommandTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendCommand_Click(this, new RoutedEventArgs());
        }

        private void UpdateTurnUI()
        {
            currentTurn = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            TurnArrow.Text = currentTurn == PieceColor.White ? "←" : "→";
        }

        private void UpdatePieceCounters()
        {
            LeftPawnCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, PieceType.Pawn)}";
            LeftKnightCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, PieceType.Knight)}";
            LeftBishopCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, PieceType.Bishop)}";
            LeftRookCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, PieceType.Rook)}";
            LeftQueenCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, PieceType.Queen)}";
            LeftKingCount.Text = $"x{chessBoard.RemainingOf(PieceColor.Black, PieceType.King)}";

            RightPawnCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, PieceType.Pawn)}";
            RightKnightCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, PieceType.Knight)}";
            RightBishopCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, PieceType.Bishop)}";
            RightRookCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, PieceType.Rook)}";
            RightQueenCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, PieceType.Queen)}";
            RightKingCount.Text = $"x{chessBoard.RemainingOf(PieceColor.White, PieceType.King)}";
        }

        private void AddMoveToSide(PieceColor color, string move)
        {
            var targetList = color == PieceColor.White ? leftMoves : rightMoves;
            if (targetList.Count >= MaxSideMoves)
                targetList.RemoveAt(0);

            targetList.Add(move);
        }

        private void StartLocal_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openBlack = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*",
                Title = "Select Black Bot",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (openBlack.ShowDialog() == true)
                blackBotPath = openBlack.FileName;

            StartMatchButton.Visibility = Visibility.Visible;
            StopMatchButton.Visibility = Visibility.Visible;
        }
     
        private void ExitLocal_Click(object sender, RoutedEventArgs e)
        {
            StartMatchButton.Visibility = Visibility.Collapsed;
            StopMatchButton.Visibility = Visibility.Collapsed;
            blackBotPath = "";
        }

        private void StartMatchButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Start match\nWhite Bot (built-in): {whiteBotPath}\nBlack Bot: {blackBotPath}", "Info");
        }

        private void StopMatchButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Stop match", "Info");
        }
    }

    public class BoardCellInfo
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }
}
