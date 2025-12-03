using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Serilog;
using Serilog.Events;

namespace Bogar.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string? _currentLogFile;

        public App()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigureLogging();
            Log.Information("Application starting. Arguments: {Args}", e.Args);
            Log.Information("Logging to {LogFile}", _currentLogFile);

            base.OnStartup(e);

            var startWindow = new StartWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            startWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application shutting down with exit code {ExitCode}", e.ApplicationExitCode);
            base.OnExit(e);
            Log.CloseAndFlush();
        }

        private void ConfigureLogging()
        {
            var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDirectory);
            _currentLogFile = Path.Combine(
                logsDirectory,
                $"bogar_{DateTimeOffset.Now:yyyyMMdd_HHmmssfff}.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Async(c => c.File(
                    _currentLogFile,
                    rollingInterval: RollingInterval.Infinite,
                    retainedFileCountLimit: 14,
                    encoding: Encoding.UTF8,
                    shared: true))
                .CreateLogger();
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unhandled dispatcher exception");
            e.Handled = false;
        }

        private static void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                Log.Fatal(exception, "Unhandled domain exception");
            }
            else
            {
                Log.Fatal("Unhandled domain exception object: {ExceptionObject}", e.ExceptionObject);
            }
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unobserved task exception");
        }
    }

}
