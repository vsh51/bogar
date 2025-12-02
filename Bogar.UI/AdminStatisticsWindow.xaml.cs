using Bogar.BLL.Statistics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Bogar.UI
{
    public partial class AdminStatisticsWindow : Window
    {
        public static readonly RoutedUICommand DeleteMatchCommand = new RoutedUICommand(
            "Delete match",
            nameof(DeleteMatchCommand),
            typeof(AdminStatisticsWindow));

        private readonly LobbyStatisticsService _statisticsService;
        private readonly string _lobbyName;
        private readonly ObservableCollection<MatchRow> _matchRows = new();
        private readonly ObservableCollection<PlayerStandingRow> _standingRows = new();

        public AdminStatisticsWindow(LobbyStatisticsService statisticsService, string lobbyName)
        {
            InitializeComponent();

            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
            _lobbyName = lobbyName;

            MatchesDataGrid.ItemsSource = _matchRows;
            PlayerStandingsList.ItemsSource = _standingRows;

            HeaderTitleText.Text = $"ADMIN STATISTICS · {lobbyName.ToUpperInvariant()}";
            StatsTitleText.Text = $"Match Statistics · {lobbyName}";

            CommandBindings.Add(new CommandBinding(DeleteMatchCommand, OnDeleteMatchCommandExecuted, OnDeleteMatchCommandCanExecute));

            Loaded += async (_, _) => await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            SetLoading(true);
            try
            {
                var historyTask = _statisticsService.GetMatchHistoryAsync();
                var snapshotTask = _statisticsService.GetLobbyStatisticsAsync();

                await Task.WhenAll(historyTask, snapshotTask);

                UpdateMatches(historyTask.Result);
                UpdateLobbyOverview(snapshotTask.Result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load statistics: {ex.Message}",
                    "Statistics", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void UpdateMatches(IReadOnlyList<MatchHistoryEntry> entries)
        {
            _matchRows.Clear();
            foreach (var entry in entries)
            {
                _matchRows.Add(new MatchRow(entry));
            }

            if (_matchRows.Count > 0)
            {
                MatchesDataGrid.SelectedIndex = 0;
            }
            else
            {
                ClearMatchDetails();
            }
        }

        private void UpdateLobbyOverview(LobbyStatisticsSnapshot snapshot)
        {
            _standingRows.Clear();
            foreach (var standing in snapshot.PlayerStandings)
            {
                _standingRows.Add(new PlayerStandingRow(standing));
            }

            StandingsEmptyText.Visibility = _standingRows.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            PlayerStandingsList.Visibility = _standingRows.Count == 0
                ? Visibility.Collapsed
                : Visibility.Visible;

            TotalMatchesValue.Text = snapshot.TotalMatches.ToString();
            DrawsValue.Text = snapshot.DrawMatches.ToString();
            AverageDurationValue.Text = snapshot.AverageDurationSeconds <= 0
                ? "—"
                : $"{snapshot.AverageDurationSeconds:F1} s";

            if (snapshot.TopPerformer != null)
            {
                TopPerformerValue.Text = $"{snapshot.TopPerformer.BotName} ({snapshot.TopPerformer.Wins} W)";
            }
            else
            {
                TopPerformerValue.Text = "—";
            }
        }

        private void SetLoading(bool isLoading)
        {
            LoadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            RefreshButton.IsEnabled = !isLoading;
        }

        private void MatchesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MatchesDataGrid.SelectedItem is MatchRow row)
            {
                ShowMatchDetails(row);
            }
            else
            {
                ClearMatchDetails();
            }
        }

        private void OnDeleteMatchCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is MatchRow;
        }

        private void OnDeleteMatchCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is MatchRow row)
            {
                _ = DeleteMatchAsync(row);
            }
        }

        private async Task DeleteMatchAsync(MatchRow row)
        {
            var result = MessageBox.Show(
                $"Delete match #{row.MatchId}?",
                "Delete match",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await _statisticsService.DeleteMatchAsync(row.MatchId);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete match: {ex.Message}",
                    "Delete match",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ShowMatchDetails(MatchRow row)
        {
            MatchDetailsPanel.Visibility = Visibility.Visible;
            MatchDetailsEmptyText.Visibility = Visibility.Collapsed;

            MatchPlayersText.Text = $"{row.WhiteBotName} vs {row.BlackBotName}";
            MatchWinnerText.Text = $"Winner: {row.WinnerName}";
            MatchScoreText.Text = $"Score • White {row.ScoreWhite} : {row.ScoreBlack} Black";
            MatchDurationText.Text = $"Duration: {row.DurationDisplay}";
            MatchStartedText.Text = $"Started: {row.StartedDisplay}";
            MatchFinishedText.Text = $"Finished: {row.FinishedDisplay}";

            MatchMovesList.ItemsSource = row.Moves;
        }

        private void ClearMatchDetails()
        {
            MatchDetailsPanel.Visibility = Visibility.Collapsed;
            MatchDetailsEmptyText.Visibility = Visibility.Visible;
            MatchMovesList.ItemsSource = null;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private sealed class MatchRow
        {
            public MatchRow(MatchHistoryEntry entry)
            {
                MatchId = entry.MatchId;
                WhiteBotName = entry.WhiteBotName;
                BlackBotName = entry.BlackBotName;
                WinnerName = entry.WinnerName;
                ScoreWhite = entry.ScoreWhite;
                ScoreBlack = entry.ScoreBlack;
                DurationSeconds = entry.DurationSeconds;
                StartedAt = entry.StartedAt;
                FinishedAt = entry.FinishedAt;
                Moves = entry.Moves?.ToList() ?? new List<string>();
            }

            public int MatchId { get; }
            public string WhiteBotName { get; }
            public string BlackBotName { get; }
            public string WinnerName { get; }
            public int ScoreWhite { get; }
            public int ScoreBlack { get; }
            public double DurationSeconds { get; }
            public DateTimeOffset StartedAt { get; }
            public DateTimeOffset? FinishedAt { get; }
            public List<string> Moves { get; }

            public string DurationDisplay => DurationSeconds <= 0 ? "—" : $"{DurationSeconds:F1} s";
            public string StartedDisplay => StartedAt.ToLocalTime().ToString("g");
            public string FinishedDisplay => FinishedAt?.ToLocalTime().ToString("g") ?? "—";
            public string MovesPreview
            {
                get
                {
                    if (Moves.Count == 0)
                        return string.Empty;

                    var preview = Moves.Take(4).ToList();
                    var suffix = Moves.Count > preview.Count ? " …" : string.Empty;
                    return string.Join(", ", preview) + suffix;
                }
            }
        }

        private sealed class PlayerStandingRow
        {
            public PlayerStandingRow(PlayerStanding standing)
            {
                BotName = standing.BotName;
                MatchesPlayed = standing.MatchesPlayed;
                Wins = standing.Wins;
                Losses = standing.Losses;
                Draws = standing.Draws;
                AverageScore = standing.AverageScore;
            }

            public string BotName { get; }
            public int MatchesPlayed { get; }
            public int Wins { get; }
            public int Losses { get; }
            public int Draws { get; }
            public double AverageScore { get; }
            public string AverageScoreDisplay => AverageScore.ToString("F1");
        }
    }
}
