using Octokit;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

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
                return false; // <-- ADDED: all code paths now return a value
            }
            catch { return false; }
        }

        public async Task ManualUpdateCheck()
        {
            // ... existing logic (will use the above methods)
        }
    }
}