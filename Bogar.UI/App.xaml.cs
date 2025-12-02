using System.Configuration;
using System.Data;
using System.Windows;

namespace Bogar.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var startWindow = new StartWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            startWindow.Show();
        }
    }

}
