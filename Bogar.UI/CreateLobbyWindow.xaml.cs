using System;
using System.Windows;
using System.Windows.Input;
using Bogar.BLL.Networking;
using Serilog;

namespace Bogar.UI
{
    public partial class CreateLobbyWindow : Window
    {
        public CreateLobbyWindow()
        {
            InitializeComponent();
        }

        private async void CreateLobby_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LobbyNameTextBox.Text))
            {
                LobbyNameError.Text = "This field is required";
                Log.Warning("Lobby creation failed validation: missing name");
                return;
            }
            else
            {
                LobbyNameError.Text = "";
            }

            if (!int.TryParse(PortTextBox.Text, out var port) || port < 1024 || port > 65535)
            {
                PortError.Text = "Enter a valid port (1024-65535)";
                Log.Warning("Lobby creation failed validation: invalid port value {PortValue}", PortTextBox.Text);
                return;
            }
            else
            {
                PortError.Text = "";
            }

            GameServer? server = null;
            try
            {
                server = new GameServer(port);
                await server.StartAsync();
                Log.Information("Server started for lobby {Lobby} on port {Port}", LobbyNameTextBox.Text, port);

                var adminWaitingRoom = new AdminWaitingRoomWindow(server, LobbyNameTextBox.Text);
                WindowNavigationHelper.Replace(this, adminWaitingRoom);
            }
            catch (Exception ex)
            {
                server?.Dispose();
                Log.Error(ex, "Failed to start server for lobby {Lobby} on port {Port}", LobbyNameTextBox.Text, port);
                MessageBox.Show($"Failed to start server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
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
            var startWindow = new StartWindow();
            startWindow.Show();
            Log.Information("Lobby creation cancelled (Close)");
            this.Close();
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var startWindow = new StartWindow();
            startWindow.Show();
            Log.Information("Lobby creation cancelled (Back)");
            this.Close();
        }


    }
}
