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
using System.Windows.Threading;
using System.Collections.ObjectModel;

namespace Bogar.UI
{
    public partial class WaitingRoomWindow : Window
    {
        public WaitingRoomWindow()
        {
            InitializeComponent();
        }

        private void LeaveLobby_Click(object sender, RoutedEventArgs e)
        {
            var joinLobbyWindow = new JoinLobbyWindow();
            joinLobbyWindow.Show();
            this.Close();
        }
    }
}
