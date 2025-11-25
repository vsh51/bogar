using System;
using System.Windows;
using System.Windows.Input;
using Bogar.BLL.Networking;

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
                return;
            }
            else
            {
                LobbyNameError.Text = "";
            }

            if (!int.TryParse(PortTextBox.Text, out var port) || port < 1024 || port > 65535)
            {
                PortError.Text = "Enter a valid port (1024-65535)";
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

                var adminWaitingRoom = new AdminWaitingRoomWindow(server, LobbyNameTextBox.Text);
                WindowNavigationHelper.Replace(this, adminWaitingRoom);
            }
            catch (Exception ex)
            {
                server?.Dispose();
                MessageBox.Show($"Failed to start server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
