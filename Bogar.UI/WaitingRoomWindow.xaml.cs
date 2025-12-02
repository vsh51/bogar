using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Bogar.BLL.Networking;
using Bogar.BLL.Core;

namespace Bogar.UI
{
    public partial class WaitingRoomWindow : Window
    {
        private readonly GameClient _client;
        private readonly ObservableCollection<string> _statusMessages = new();
        private readonly string _lobbyName;
        private readonly string _lobbyIp;
        private readonly int _lobbyPort;
        private readonly string _userName;
        private bool _isLeaving;

        public WaitingRoomWindow(GameClient client, string lobbyName, string lobbyIp, int lobbyPort, string userName)
        {
            InitializeComponent();
            _client = client;
            _lobbyName = lobbyName;
            _lobbyIp = lobbyIp;
            _lobbyPort = lobbyPort;
            _userName = userName;

            LobbyNameText.Text = string.IsNullOrWhiteSpace(lobbyName) ? "Lobby" : $"Lobby: {lobbyName}";
            LobbyIpText.Text = $"IP: {_lobbyIp}:{_lobbyPort}";
            PlayerNameText.Text = $"You: {_userName}";
            StatusListBox.ItemsSource = _statusMessages;
            _statusMessages.Add("Connected to server.");

            _client.LogMessage += OnClientLog;
            _client.MatchPreparing += OnMatchPreparing;
            _client.GameStarted += OnGameStarted;
            _client.GameEnded += OnGameEnded;
            _client.ErrorReceived += OnError;
            _client.Disconnected += OnDisconnected;
        }

        private void OnClientLog(string message) => Dispatcher.Invoke(() => AddStatus(message));

        private void OnMatchPreparing(string opponent)
        {
            Dispatcher.Invoke(() =>
            {
                AddStatus($"Match will start soon against {opponent}.");
                StatusText.Text = $"Preparing match vs {opponent}";
            });
        }

        private void OnGameStarted(string opponent, Color color)
        {
            Dispatcher.Invoke(() =>
            {
                AddStatus($"Game started vs {opponent}. You play as {color}.");
                StatusText.Text = $"Playing as {color} vs {opponent}";
            });
        }

        private void OnGameEnded(string result)
        {
            Dispatcher.Invoke(() =>
            {
                AddStatus($"Game ended: {result}");
                StatusText.Text = result;
            });
        }

        private void OnError(string error)
        {
            Dispatcher.Invoke(() =>
            {
                AddStatus($"Error: {error}");
                StatusText.Text = error;

                if (_isLeaving)
                {
                    NavigateToMainMenu();
                    return;
                }

                MessageBox.Show(error, "Network Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigateToMainMenu();
            });
        }

        private void AddStatus(string message)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _statusMessages.Add(entry);
            if (_statusMessages.Count > 200)
                _statusMessages.RemoveAt(0);
        }

        private void LeaveLobby_Click(object sender, RoutedEventArgs e)
        {
            _isLeaving = true;
            NavigateToMainMenu();
        }

        private bool _navigatedToMainMenu;

        private void NavigateToMainMenu()
        {
            if (_navigatedToMainMenu)
                return;

            _navigatedToMainMenu = true;
            _client.Disconnect();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var startWindow = new StartWindow();
                WindowNavigationHelper.Replace(this, startWindow);
            }));
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OnDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                if (_isLeaving)
                    return;
                StatusText.Text = "Server is down.";
                MessageBox.Show("The server has been shut down.", "Disconnected", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigateToMainMenu();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _client.LogMessage -= OnClientLog;
            _client.MatchPreparing -= OnMatchPreparing;
            _client.GameStarted -= OnGameStarted;
            _client.GameEnded -= OnGameEnded;
            _client.ErrorReceived -= OnError;
            _client.Disconnected -= OnDisconnected;
            _client.Dispose();
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
            _isLeaving = true;
            NavigateToMainMenu();
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var joinLobbyWindow = new JoinLobbyWindow();
            joinLobbyWindow.Show();
            this.Close();
        }

    }
}
