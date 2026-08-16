using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Http;

namespace Un0
{
    public partial class App : Application
    {
        private static Mutex _mutex = null;
        private const string AppMutexName = "Un0_App_Mutex";
        private static bool _isAppStarting = false;
        public static bool IsLatestVersion { get; set; } = false;

        public App()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    var versionString = $"{version.Major}.{version.Minor}.{version.Build}";
                    Debug.WriteLine($"Version set: {versionString}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"App initialization error: {ex.Message}");
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Prevent multiple startup calls
            if (_isAppStarting)
            {
                Debug.WriteLine("App already starting, ignoring duplicate call.");
                return;
            }
            _isAppStarting = true;

            try
            {
                base.OnStartup(e);

                // Check if another instance is already running
                bool isNewInstance;
                _mutex = new Mutex(true, AppMutexName, out isNewInstance);

                if (!isNewInstance)
                {
                    Debug.WriteLine("Another instance is already running. Exiting.");
                    Shutdown();
                    return;
                }

                Debug.WriteLine("App started successfully.");

                // Create and show main window
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
            finally
            {
                _isAppStarting = false;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            base.OnExit(e);
        }
    }
}