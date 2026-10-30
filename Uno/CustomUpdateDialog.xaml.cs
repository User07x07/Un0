using System;
using System.Collections.Generic;
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

            // Set release notes - turn GitHub markdown into a plain bullet list
            ReleaseNotesText.Text = FormatReleaseNotes(releaseNotes);
        }

        /// <summary>
        /// Turns a raw GitHub release body (markdown) into a clean bullet list:
        /// headings become labels, list items become bullets, inline markers are removed.
        /// </summary>
        private static string FormatReleaseNotes(string releaseNotes)
        {
            const string fallback = "• Bug fixes and performance improvements";

            if (string.IsNullOrWhiteSpace(releaseNotes) ||
                releaseNotes.Trim() == "No release notes available.")
            {
                return fallback;
            }

            var lines = new List<string>();

            foreach (var rawLine in releaseNotes.Replace("\r\n", "\n").Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                // Skip horizontal rules and HTML/markdown noise
                if (line.StartsWith("---") || line.StartsWith("***") ||
                    line.StartsWith("<img") || line.StartsWith("![") ||
                    line.StartsWith("[![") || line.StartsWith("<!--"))
                {
                    continue;
                }

                // Headings: keep the words as a label, no hashes
                if (line.StartsWith("#"))
                {
                    var heading = CleanInline(line.TrimStart('#').Trim()).TrimEnd(':');
                    if (heading.Length > 0)
                    {
                        // Blank line before a new group, so groups read apart
                        if (lines.Count > 1 && lines[lines.Count - 1].Length > 0)
                        {
                            lines.Add(string.Empty);
                        }

                        lines.Add(heading.ToUpperInvariant());
                    }
                    continue;
                }

                // List items: - * + or 1.
                bool isListItem = line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("+ ");
                if (!isListItem)
                {
                    var numbered = System.Text.RegularExpressions.Regex.Match(line, @"^\d+\.\s+");
                    if (numbered.Success)
                    {
                        line = line.Substring(numbered.Length);
                        isListItem = true;
                    }
                }

                if (isListItem)
                {
                    line = line.Substring(2).Trim();
                }

                var text = CleanInline(line);
                if (text.Length == 0)
                {
                    continue;
                }

                lines.Add("• " + text);
            }

            if (lines.Count == 0)
            {
                return fallback;
            }

            // Keep the card compact: at most 8 content lines plus group labels
            while (lines.Count > 10)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join("\n", lines).Trim();
        }

        /// <summary>
        /// Removes inline markdown (bold, italic, code, links) from one line.
        /// </summary>
        private static string CleanInline(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var result = text;

            // Links: [label](url) -> label
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\[([^\]]+)\]\([^)]*\)", "$1");

            // Bold / italic / strikethrough / inline code markers
            result = result.Replace("**", string.Empty)
                           .Replace("__", string.Empty)
                           .Replace("~~", string.Empty)
                           .Replace("`", string.Empty);

            result = result.Trim('*', '_', '•', ' ');

            // Collapse repeated spaces
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s{2,}", " ");

            return result.Trim();
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