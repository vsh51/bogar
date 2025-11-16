using System;
using System.Windows;
using Microsoft.Win32;

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
            var dlg = new OpenFileDialog();
            dlg.Filter = "Executable Files (*.exe)|*.exe";
            if (dlg.ShowDialog() == true)
            {
                BotExeTextBox.Text = dlg.FileName;
            }
        }

        private void JoinLobby_Click(object sender, RoutedEventArgs e)
        {
            bool valid = true;

            if (string.IsNullOrWhiteSpace(LobbyIpTextBox.Text))
            {
                LobbyIpError.Text = "This field is required";
                valid = false;
            }
            else
            {
                LobbyIpError.Text = "";
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

            if (!valid)
                return;

            var waitingRoom = new WaitingRoomWindow(); waitingRoom.Show();
            this.Close();
        }


        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            var startWindow = new StartWindow();
            startWindow.Show();
            this.Close();
        }
    }
}
