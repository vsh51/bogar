using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bogar.UI
{
    public partial class BotSelectionDialog : Window
    {
        public string WhiteBotPath { get; private set; } = "";
        public string BlackBotPath { get; private set; } = "";

        public BotSelectionDialog()
        {
            InitializeComponent();
        }

        private void BrowseWhiteBot_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select White Bot",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                WhiteBotPath = openFileDialog.FileName;
                WhiteBotTextBox.Text = System.IO.Path.GetFileName(WhiteBotPath);
                ValidateSelection();
            }
        }

        private void BrowseBlackBot_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Black Bot",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                BlackBotPath = openFileDialog.FileName;
                BlackBotTextBox.Text = System.IO.Path.GetFileName(BlackBotPath);
                ValidateSelection();
            }
        }

        private void ValidateSelection()
        {
            ValidationMessage.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrEmpty(BlackBotPath))
            {
                StartButton.IsEnabled = true;
            }
            else
            {
                StartButton.IsEnabled = false;
            }
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(BlackBotPath))
            {
                ValidationMessage.Text = "Please select at least a Black bot.";
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
