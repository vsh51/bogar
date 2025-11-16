using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public partial class AdminWaitingRoomWindow : Window
    {
        public AdminWaitingRoomWindow(string lobbyName, string lobbyIp)
        {
            InitializeComponent();
            LobbyNameText.Text = $"Lobby: {lobbyName}";
            LobbyIpText.Text = $"IP: {lobbyIp}";
        }

        private void StartMatch_Click(object sender, RoutedEventArgs e)
        {
            var adminMatchWindow = new AdminMatchWindow();
            adminMatchWindow.Show();
            this.Close();
        }

        private void DeleteLobby_Click(object sender, RoutedEventArgs e)
        {
            var startWindow = new StartWindow();
            startWindow.Show();
            this.Close();
        }
    }
}

