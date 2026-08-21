using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace Un0
{
    public partial class App : Application
    {
        private static Mutex _mutex;
        private const string AppMutexName = "Un0_SingleInstance_Mutex";

        public static bool IsLatestVersion { get; set; } = false;

        public App()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    Debug.WriteLine($"Version set: {version.Major}.{version.Minor}.{version.Build}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"App initialization error: {ex.Message}");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Try to create the mutex. If it already exists, another instance is running.
                bool createdNew;
                _mutex = new Mutex(true, AppMutexName, out createdNew);

                if (!createdNew)
                {
                    // Bring the existing window forward
                    IntPtr existingHwnd = SingleInstance.ActivateExistingWindow();

                    // Show non-blocking notification
                    var dialog = new NotificationDialog();
                    dialog.CenterOverWindow(existingHwnd);

                    // When the notification closes, shut down this instance
                    dialog.Closed += (s, args) => Shutdown();

                    dialog.Show();

                    return; // Don't block — let the message pump run
                }


                // Normal startup
                base.OnStartup(e);

                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error starting application: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
                _mutex = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Mutex release error: {ex.Message}");
            }
            base.OnExit(e);
        }
    }
}