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
    /// <summary>
    /// Interaction logic for AdminMatchWindow.xaml
    /// </summary>
    public partial class AdminMatchWindow : Window
    {
        public AdminMatchWindow()
        {
            InitializeComponent();
        }

        private void LeaveMatch_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void StartMatchButton_Click(object sender, RoutedEventArgs e) {
            // pass
        }
        private void StartNextMatch_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement start next match logic
        }

        private void ViewStatistics_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement view lobby statistics logic
        }

        private void LeaveLobby_Click(object sender, RoutedEventArgs e)
        {
            var startWindow = new StartWindow();
            startWindow.Show();
            this.Close();
        }

        private void StopMatch_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement stop match logic
        }

        private void ResumeMatch_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement resume match logic
        }
    }
}
