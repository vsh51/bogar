using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Bogar.BLL.Networking;

namespace Bogar.UI
{
    public partial class JoinLobbyWindow : Window
    {
        public JoinLobbyWindow()
        {
            InitializeComponent();
        }

        private void BrowseBotExe_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe"
            };
            if (dlg.ShowDialog() == true)
            {
                BotExeTextBox.Text = dlg.FileName;
            }
        }

        private async void JoinLobby_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out var port))
                return;

            JoinLobbyButton.IsEnabled = false;

            try
            {
                var client = new GameClient();
                bool connected = await client.ConnectAsync(
                    LobbyIpTextBox.Text.Trim(),
                    port,
                    UserNameTextBox.Text.Trim(),
                    BotExeTextBox.Text.Trim());

                if (connected)
                {
                    var waitingRoom = new WaitingRoomWindow(
                        client,
                        LobbyNameTextBox.Text.Trim(),
                        LobbyIpTextBox.Text.Trim(),
                        port,
                        UserNameTextBox.Text.Trim());
                    waitingRoom.Show();
                    Close();
                }
                else
                {
                    client.Dispose();
                    MessageBox.Show("Failed to connect to server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    JoinLobbyButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                JoinLobbyButton.IsEnabled = true;
            }
        }

        private bool ValidateInputs(out int port)
        {
            bool valid = true;
            port = 0;

            if (string.IsNullOrWhiteSpace(LobbyIpTextBox.Text))
            {
                LobbyIpError.Text = "This field is required";
                valid = false;
            }
            else
            {
                LobbyIpError.Text = "";
            }

            if (!int.TryParse(PortTextBox.Text, out port) || port < 1024 || port > 65535)
            {
                PortError.Text = "Enter a valid port (1024-65535)";
                valid = false;
            }
            else
            {
                PortError.Text = "";
            }

            if (string.IsNullOrWhiteSpace(LobbyNameTextBox.Text))
            {
                LobbyNameError.Text = "This field is required";
                valid = false;
            }
            else
            {
                LobbyNameError.Text = "";
            }

            if (string.IsNullOrWhiteSpace(BotExeTextBox.Text))
            {
                BotExeError.Text = "This field is required";
                valid = false;
            }
            else
            {
                BotExeError.Text = "";
            }

            if (string.IsNullOrWhiteSpace(UserNameTextBox.Text))
            {
                UserNameError.Text = "This field is required";
                valid = false;
            }
            else
            {
                UserNameError.Text = "";
            }

            return valid;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            var startWindow = new StartWindow();
            startWindow.Show();
            Close();
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
