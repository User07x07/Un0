using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Un0
{
    public partial class CustomUpdateDialog : Window
    {
        public bool IsUpdateConfirmed { get; private set; } = false;

        // Constructor with 3 parameters (removed the 4th parameter)
        public CustomUpdateDialog(string currentVersion, string latestVersion, string releaseNotes)
        {
            InitializeComponent();

            // Handle null values
            CurrentVersionText.Text = $"v{currentVersion ?? "1.0.0"}";
            LatestVersionText.Text = $"v{latestVersion ?? "1.3.0"}";

            if (!string.IsNullOrEmpty(releaseNotes) && releaseNotes != "No release notes available.")
            {
                var notes = releaseNotes.Length > 150 ? releaseNotes.Substring(0, 150) + "..." : releaseNotes;
                ReleaseNotesText.Text = $"• {notes}";
            }
            else
            {
                ReleaseNotesText.Text = "• Bug fixes and performance improvements";
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            IsUpdateConfirmed = true;

            // Open the download website in browser
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/User07x07/Un0/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening browser: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            this.DialogResult = true;
            this.Close();

            // Exit the entire application
            System.Windows.Application.Current.Shutdown();
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn && btn == UpdateButton)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(0, 220, 120));
            }
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn && btn == UpdateButton)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(0, 255, 136));
            }
        }
    }
}