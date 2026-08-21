using Octokit;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace Un0.Services
{
    public class UpdateService
    {
        private const string GITHUB_OWNER = "User07x07";
        private const string GITHUB_REPO = "Un0";
        private GitHubClient _client;
        private Octokit.Release _latestRelease;
        private bool _isUpdateAvailable = false;
        private StatusBarService _statusBar;
        private bool _isChecking = false;

        public event EventHandler<bool> UpdateAvailabilityChanged;

        public UpdateService(StatusBarService statusBar)
        {
            _statusBar = statusBar;
            InitializeGitHubClient();
        }

        private void InitializeGitHubClient()
        {
            try
            {
                _client = new GitHubClient(new ProductHeaderValue("Un0-App"));
                _client.SetRequestTimeout(TimeSpan.FromSeconds(10));
            }
            catch { _client = null; }
        }

        public async Task CheckForUpdatesOnLoad()
        {
            bool hasUpdate = await CheckForUpdatesAsync();
            UpdateAvailabilityChanged?.Invoke(this, hasUpdate);
            if (!hasUpdate)
            {
                App.IsLatestVersion = true;
            }
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                if (_client == null) return false;

                var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var currentVersionString = currentVersion != null ? $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}" : "1.0.0";

                var releases = await _client.Repository.Release.GetAll(GITHUB_OWNER, GITHUB_REPO);
                if (releases.Count > 0)
                {
                    _latestRelease = releases[0];
                    var latestVersion = _latestRelease.TagName.Replace("v", "");
                    if (IsNewerVersion(latestVersion, currentVersionString))
                    {
                        _isUpdateAvailable = true;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetLatestVersionAsync()
        {
            try
            {
                if (_client == null) return "1.3.0";
                var releases = await _client.Repository.Release.GetAll(GITHUB_OWNER, GITHUB_REPO);
                if (releases.Count > 0)
                    return releases[0].TagName.Replace("v", "");
                return "1.3.0";
            }
            catch { return "1.3.0"; }
        }

        public async Task<string> GetLatestReleaseNotesAsync()
        {
            try
            {
                if (_client == null) return "";
                var releases = await _client.Repository.Release.GetAll(GITHUB_OWNER, GITHUB_REPO);
                if (releases.Count > 0)
                    return releases[0].Body ?? "No release notes available.";
                return "";
            }
            catch { return ""; }
        }

        public bool IsNewerVersion(string latest, string current)
        {
            try
            {
                var latestParts = latest.Split('.');
                var currentParts = current.Split('.');
                for (int i = 0; i < Math.Max(latestParts.Length, currentParts.Length); i++)
                {
                    int latestNum = i < latestParts.Length ? int.Parse(latestParts[i]) : 0;
                    int currentNum = i < currentParts.Length ? int.Parse(currentParts[i]) : 0;
                    if (latestNum > currentNum) return true;
                    if (latestNum < currentNum) return false;
                }
                return false;
            }
            catch { return false; }
        }

        public async Task ManualUpdateCheck()
        {
            if (_isChecking) return;
            _isChecking = true;

            try
            {
                _statusBar.SetStatus(" Checking for updates...", "#FFD93D");

                bool hasUpdate = await CheckForUpdatesAsync();

                if (hasUpdate)
                {
                    var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    var currentVersionString = currentVersion != null ? $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}" : "1.0.0";
                    var latestVersion = await GetLatestVersionAsync() ?? "1.3.0";
                    var releaseNotes = await GetLatestReleaseNotesAsync() ?? "New features and improvements";

                    var updateDialog = new CustomUpdateDialog(currentVersionString, latestVersion, releaseNotes);
                    updateDialog.ShowDialog();

                    if (updateDialog.IsUpdateConfirmed)
                    {
                        _statusBar.SetStatus(" Opening download page...", "#FFD93D");

                        try
                        {
                            // Open the download URL (adjust if needed)
                            // Process.Start(new ProcessStartInfo { FileName = "https://un0officialaccess.netlify.app/", UseShellExecute = true });
                            // For now, we just open a browser with the release page
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = $"https://github.com/{GITHUB_OWNER}/{GITHUB_REPO}/releases",
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error opening browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            _statusBar.SetStatus(" Error opening download page", "#FF4444");
                            await Task.Delay(1500);
                        }

                        await Task.Delay(2000);
                        _statusBar.SetStatus("Ready", "#666666");
                    }
                    else
                    {
                        _statusBar.SetStatus(" Update cancelled", "#666666");
                        await Task.Delay(1500);
                        _statusBar.SetStatus("Ready", "#666666");
                    }
                }
                else
                {
                    _statusBar.SetStatus(" ✓ You have the latest version", "#00FF88");
                    App.IsLatestVersion = true;
                    // Optionally update the UI to show "Latest" label via an event or direct call
                    // We'll raise an event to notify that we are up-to-date
                    UpdateAvailabilityChanged?.Invoke(this, false);
                    await Task.Delay(3000);
                    _statusBar.SetStatus("Ready", "#666666");
                }
            }
            catch (Exception ex)
            {
                _statusBar.SetStatus(" Update check failed", "#FF4444");
                await Task.Delay(2000);
                _statusBar.SetStatus("Ready", "#666666");
                Debug.WriteLine($"Manual update check error: {ex.Message}");
            }
            finally
            {
                _isChecking = false;
            }
        }
    }
}