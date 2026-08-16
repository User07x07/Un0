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

        public CustomUpdateDialog(string currentVersion, string latestVersion, string releaseNotes)
        {
            InitializeComponent();

            // Set owner to center on parent window
            this.Owner = App.Current.MainWindow;

            // Set version info
            CurrentVersionText.Text = $"v{currentVersion}";
            LatestVersionText.Text = $"v{latestVersion}";

            // Set release notes - properly formatted with bullet points
            if (!string.IsNullOrEmpty(releaseNotes) && releaseNotes != "No release notes available.")
            {
                // Clean up release notes - remove any existing bullet symbols
                var cleanNotes = releaseNotes.Replace("•", "").Trim();

                // Split by newlines or periods for multiple items
                var items = cleanNotes.Split(new[] { '\n', '\r', '.' }, StringSplitOptions.RemoveEmptyEntries);

                if (items.Length > 1)
                {
                    // Multiple items - show as list
                    string formattedNotes = "";
                    for (int i = 0; i < Math.Min(items.Length, 3); i++)
                    {
                        if (!string.IsNullOrWhiteSpace(items[i]))
                        {
                            formattedNotes += $"• {items[i].Trim()}\n";
                        }
                    }
                    ReleaseNotesText.Text = formattedNotes.TrimEnd('\n');
                }
                else
                {
                    // Single item
                    ReleaseNotesText.Text = $"• {cleanNotes}";
                }

                // Truncate if too long
                if (ReleaseNotesText.Text.Length > 200)
                {
                    ReleaseNotesText.Text = ReleaseNotesText.Text.Substring(0, 200) + "...";
                }
            }
            else
            {
                ReleaseNotesText.Text = "• Bug fixes and performance improvements";
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            IsUpdateConfirmed = true;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://un0officialaccess.netlify.app/",
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsUpdateConfirmed = false;
            this.DialogResult = false;
            this.Close();
        }

        private void CloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(232, 17, 35));
                btn.Foreground = Brushes.White;
            }
        }

        private void CloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Background = Brushes.Transparent;
                btn.Foreground = (Brush)new BrushConverter().ConvertFromString("#666666");
            }
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