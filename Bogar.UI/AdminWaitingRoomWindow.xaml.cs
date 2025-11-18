using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Bogar.BLL.Networking;
using Bogar.BLL.Core;

namespace Bogar.UI
{
    public partial class AdminWaitingRoomWindow : Window
    {
        private readonly GameServer _server;
        private readonly ObservableCollection<string> _logMessages = new();
        private readonly ObservableCollection<ClientListItem> _clients = new();
        private readonly DispatcherTimer _refreshTimer;
        private AdminMatchWindow? _matchWindow;
        private bool _isRefreshingClients;

        public AdminWaitingRoomWindow(GameServer server, string lobbyName)
        {
            InitializeComponent();

            _server = server;
            LobbyNameText.Text = $"Lobby: {lobbyName}";
            LobbyIpText.Text = $"IP: {GetLocalIPAddress()}";
            LobbyPortText.Text = $"Port: {_server.Port}";

            LogListBox.ItemsSource = _logMessages;
            ClientsListBox.ItemsSource = _clients;

            _server.LogMessage += OnServerLog;
            _server.ClientConnected += OnClientChanged;
            _server.ClientDisconnected += OnClientChanged;
            _server.GameStarted += OnGameStarted;
            _server.GameEnded += OnGameEnded;

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _refreshTimer.Tick += (s, e) => RefreshClients();
            _refreshTimer.Start();

            _logMessages.Add("Waiting for clients to connect...");
            RefreshClients();
        }

        private void OnServerLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                _logMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                if (_logMessages.Count > 200)
                    _logMessages.RemoveAt(0);
            });
        }

        private void OnClientChanged(ConnectedClient client)
        {
            Dispatcher.Invoke(() =>
            {
                RefreshClients();
                _logMessages.Add($"[{DateTime.Now:HH:mm:ss}] Client {(client.TcpClient.Connected ? "connected" : "disconnected")}: {client.Nickname}");
            });
        }

        private void OnGameStarted(ConnectedClient white, ConnectedClient black)
        {
            Dispatcher.Invoke(() =>
            {
                _logMessages.Add($"[{DateTime.Now:HH:mm:ss}] Game started: {white.Nickname} vs {black.Nickname}");
                RefreshClients();
            });
        }

        private void OnGameEnded(ConnectedClient white, ConnectedClient black, Color? winner)
        {
            Dispatcher.Invoke(() =>
            {
                string result = winner switch
                {
                    Color.White => $"{white.Nickname} wins",
                    Color.Black => $"{black.Nickname} wins",
                    _ => "Draw"
                };
                _logMessages.Add($"[{DateTime.Now:HH:mm:ss}] Game ended: {result}");
                RefreshClients();
            });
        }

        private void RefreshClients()
        {
            _isRefreshingClients = true;
            var previouslySelected = ClientsListBox.SelectedItems
                .OfType<ClientListItem>()
                .Select(i => i.Client.Id)
                .ToHashSet();

            _clients.Clear();

            foreach (var client in _server.GetConnectedClients())
            {
                bool inGame = _server.IsClientInGame(client.Id);
                _clients.Add(new ClientListItem(client, inGame));
            }

            ClientsListBox.SelectedItems.Clear();
            foreach (var item in _clients)
            {
                if (previouslySelected.Contains(item.Client.Id) && !item.IsInGame)
                {
                    ClientsListBox.SelectedItems.Add(item);
                }
            }

            NoClientsText.Visibility = _clients.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            _isRefreshingClients = false;
            UpdateStartMatchButtonState();
        }

        private void ClientsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isRefreshingClients)
                return;

            foreach (var item in e.AddedItems.OfType<ClientListItem>().Where(i => i.IsInGame).ToList())
            {
                ClientsListBox.SelectedItems.Remove(item);
            }

            UpdateStartMatchButtonState();
        }

        private void UpdateStartMatchButtonState()
        {
            var selectable = ClientsListBox.SelectedItems
                .OfType<ClientListItem>()
                .Where(i => !i.IsInGame)
                .ToList();

            StartMatchButton.IsEnabled = selectable.Count == 2;
        }

        private void StartMatchButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ClientsListBox.SelectedItems
                .OfType<ClientListItem>()
                .Where(i => !i.IsInGame)
                .ToList();

            if (selected.Count != 2)
                return;

            var client1 = selected[0].Client;
            var client2 = selected[1].Client;

            if (_server.TryStartMatch(client1.Id, client2.Id, out var error))
            {
                _logMessages.Add($"[{DateTime.Now:HH:mm:ss}] Match queued: {client1.Nickname} vs {client2.Nickname}");
                ClientsListBox.UnselectAll();
                _matchWindow?.Close();
                _matchWindow = new AdminMatchWindow(_server, client1.Id, client2.Id, client1.Nickname, client2.Nickname);
                _matchWindow.Show();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "Cannot start match", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            RefreshClients();
        }

        private void DeleteLobby_Click(object sender, RoutedEventArgs e)
        {
            Close();
            var startWindow = new StartWindow();
            startWindow.Show();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                return ip?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _refreshTimer?.Stop();

            _server.LogMessage -= OnServerLog;
            _server.ClientConnected -= OnClientChanged;
            _server.ClientDisconnected -= OnClientChanged;
            _server.GameStarted -= OnGameStarted;
            _server.GameEnded -= OnGameEnded;

            _matchWindow?.Close();
            _server.Dispose();
        }

        private sealed class ClientListItem
        {
            public ClientListItem(ConnectedClient client, bool isInGame)
            {
                Client = client;
                IsInGame = isInGame;
            }

            public ConnectedClient Client { get; }
            public bool IsInGame { get; }

            public string DisplayText => $"{Client.Nickname}{(IsInGame ? " (In Game)" : " (Idle)")}";
        }
    }
}
