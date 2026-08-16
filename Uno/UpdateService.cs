using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Octokit;

namespace Un0
{
    public class UpdateService
    {
        private const string GITHUB_OWNER = "User07x07";
        private const string GITHUB_REPO = "Un0";
        private const string DOWNLOAD_URL = "https://un0officialaccess.netlify.app/";

        public static string CurrentVersion { get; set; } = "1.2.0";

        private GitHubClient _client;
        private bool _isUpdateAvailable = false;
        private Octokit.Release _latestRelease;

        public UpdateService()
        {
            _client = new GitHubClient(new ProductHeaderValue("Un0-App"));
        }

        public static void SetVersion(string version)
        {
            CurrentVersion = version;
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                Debug.WriteLine($"Checking for updates... Current version: {CurrentVersion}");

                var releases = await _client.Repository.Release.GetAll(GITHUB_OWNER, GITHUB_REPO);

                Debug.WriteLine($"Found {releases.Count} releases");

                if (releases.Count > 0)
                {
                    _latestRelease = releases[0];

                    var latestVersion = _latestRelease.TagName.Replace("v", "");
                    var currentVersion = CurrentVersion;

                    Debug.WriteLine($"Latest version: {latestVersion}");
                    Debug.WriteLine($"Current version: {currentVersion}");

                    if (IsNewerVersion(latestVersion, currentVersion))
                    {
                        _isUpdateAvailable = true;
                        Debug.WriteLine("Update available!");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("No update available");
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
                var releases = await _client.Repository.Release.GetAll(GITHUB_OWNER, GITHUB_REPO);
                if (releases.Count > 0)
                {
                    return releases[0].TagName.Replace("v", "");
                }
                return CurrentVersion;
            }
            catch
            {
                return CurrentVersion;
            }
        }

        private bool IsNewerVersion(string latest, string current)
        {
            try
            {
                var latestParts = latest.Split('.');
                var currentParts = current.Split('.');

                for (int i = 0; i < Math.Max(latestParts.Length, currentParts.Length); i++)
                {
                    int latestNum = i < latestParts.Length ? int.Parse(latestParts[i]) : 0;
                    int currentNum = i < currentParts.Length ? int.Parse(currentParts[i]) : 0;

                    if (latestNum > currentNum)
                        return true;
                    if (latestNum < currentNum)
                        return false;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DownloadAndInstallUpdateAsync()
        {
            try
            {
                if (_latestRelease == null || !_isUpdateAvailable)
                    return false;

                var result = MessageBox.Show(
                    $"A new version is available!\n\n" +
                    $"Current version: v{CurrentVersion}\n" +
                    $"Latest version: {_latestRelease.TagName}\n\n" +
                    $"Release Notes:\n{_latestRelease.Body ?? "No release notes available"}\n\n" +
                    $"Please download the latest version from:\n{DOWNLOAD_URL}\n\n" +
                    "Would you like to go to the download page?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = DOWNLOAD_URL,
                        UseShellExecute = true
                    });
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}",
                    "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async Task ShowUpdateNotificationAsync()
        {
            if (await CheckForUpdatesAsync())
            {
                var result = MessageBox.Show(
                    $"A new version is available!\n\n" +
                    $"Current: v{CurrentVersion}\n" +
                    $"Latest: {_latestRelease.TagName}\n\n" +
                    $"Release Notes:\n{_latestRelease.Body ?? "No release notes"}\n\n" +
                    $"Please download the latest version from:\n{DOWNLOAD_URL}\n\n" +
                    "Do you want to go to the download page?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    await DownloadAndInstallUpdateAsync();
                }
            }
        }

        public async Task CheckAndNotifyAsync()
        {
            if (await CheckForUpdatesAsync())
            {
                var result = MessageBox.Show(
                    $"New Release Available! 🎉\n\n" +
                    $"Current version: v{CurrentVersion}\n" +
                    $"Latest version: {_latestRelease.TagName}\n\n" +
                    $"Please download the latest version from:\n{DOWNLOAD_URL}\n\n" +
                    "Would you like to go to the download page now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = DOWNLOAD_URL,
                        UseShellExecute = true
                    });
                }
            }
        }

        public async Task<bool> ForceUpdateIfAvailableAsync()
        {
            if (await CheckForUpdatesAsync())
            {
                var result = MessageBox.Show(
                    $"⚠️ UPDATE REQUIRED ⚠️\n\n" +
                    $"Your version: v{CurrentVersion}\n" +
                    $"Latest version: {_latestRelease.TagName}\n\n" +
                    $"You must update to the latest version to continue using Un0.\n\n" +
                    $"Please download the latest version from:\n{DOWNLOAD_URL}\n\n" +
                    "Would you like to go to the download page now?",
                    "Update Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = DOWNLOAD_URL,
                        UseShellExecute = true
                    });
                }

                return false;
            }

            return true;
        }
    }
}