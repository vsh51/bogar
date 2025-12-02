using System;
using System.Windows;
using Microsoft.Win32;

namespace Bogar.UI
{
    public partial class StartWindow : Window
    {
        private string whiteBotPath = "";
        private string blackBotPath = "";

        public StartWindow()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BrowseWhiteBotButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select White Bot"
            };

            if (dialog.ShowDialog() == true)
            {
                whiteBotPath = dialog.FileName;
                WhiteBotPathTextBox.Text = System.IO.Path.GetFileName(whiteBotPath);
                UpdateStartLocalGameButtonState();
            }
        }

        private void BrowseBlackBotButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Black Bot"
            };

            if (dialog.ShowDialog() == true)
            {
                blackBotPath = dialog.FileName;
                BlackBotPathTextBox.Text = System.IO.Path.GetFileName(blackBotPath);
                UpdateStartLocalGameButtonState();
            }
        }

        private void UpdateStartLocalGameButtonState()
        {
            StartLocalGameButton.IsEnabled = !string.IsNullOrEmpty(blackBotPath);
        }

        private void StartLocalGameButton_Click(object sender, RoutedEventArgs e)
        {
            var matchWindow = new MatchWindow(whiteBotPath, blackBotPath);
            WindowNavigationHelper.Replace(this, matchWindow);
        }

        private void HostTournamentButton_Click(object sender, RoutedEventArgs e)
        {
            var createLobbyWindow = new CreateLobbyWindow();
            WindowNavigationHelper.Replace(this, createLobbyWindow);
        }


        private void JoinLobbyButton_Click(object sender, RoutedEventArgs e)
        {
            var joinLobbyWindow = new JoinLobbyWindow();
            WindowNavigationHelper.Replace(this, joinLobbyWindow);
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
            this.Close();
        }

    }
}
