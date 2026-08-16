using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace Un0
{
    public partial class App : Application
    {
        private static Mutex _mutex = null;
        private const string AppMutexName = "Global\\Un0_App_Mutex";
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
            try
            {
                base.OnStartup(e);

                // Check if any Un0 instance is already running (process or DLL)
                if (SingleInstance.IsUn0Running())
                {
                    Debug.WriteLine("Another Un0 instance detected (process or DLL). Shutting down.");
                    MessageBox.Show("Un0 is already running.\n\nOnly one instance can run at a time.",
                        "Already Running", MessageBoxButton.OK, MessageBoxImage.Information);
                    Shutdown();
                    return;
                }

                // Mutex for additional safety
                bool createdNew;
                _mutex = new Mutex(true, AppMutexName, out createdNew);

                if (!createdNew)
                {
                    Shutdown();
                    return;
                }

                Debug.WriteLine("App started successfully - No other instances found.");

                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                    _mutex = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Mutex release error: {ex.Message}");
            }
            base.OnExit(e);
        }
    }
}