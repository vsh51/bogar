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
    public partial class CreateLobbyWindow : Window
    {
        public CreateLobbyWindow()
        {
            InitializeComponent();
        }

        private void CreateLobby_Click(object sender, RoutedEventArgs e)
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

            // Proceed to admin waiting room
            var adminWaitingRoom = new AdminWaitingRoomWindow(LobbyNameTextBox.Text, "192.168.1.100");
            adminWaitingRoom.Show();
            this.Close();
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

