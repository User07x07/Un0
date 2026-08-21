using Microsoft.Web.WebView2.Core;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Un0.Services
{
    public class NavigationService
    {
        private WebViewManager _webViewManager;
        private Button _backButton;
        private StatusBarService _statusBar;
        private DispatcherTimer _statusTimer;

        public NavigationService(WebViewManager webViewManager, Button backButton, StatusBarService statusBar)
        {
            _webViewManager = webViewManager;
            _backButton = backButton;
            _statusBar = statusBar;

            _webViewManager.NavigationStarting += (s, e) => UpdateButtons();
            _webViewManager.NavigationCompleted += (s, e) =>
            {
                UpdateButtons();
                // Reset status after navigation completes (if not showing a special message)
                _statusBar.SetStatus("Ready", "#666666");
            };
        }

        public void GoBack()
        {
            if (_webViewManager.CanGoBack)
            {
                _webViewManager.GoBack();
                _statusBar.SetStatus("Going back...", "#00FF88");
            }
        }

        public void Refresh()
        {
            _webViewManager.Reload();
            _statusBar.SetStatus("🔄 Refreshing...", "#FFD93D");
            // After refresh, the navigation completed event will set status to Ready.
        }

        public void GoHome()
        {
            _webViewManager.NavigateTo("https://mainframe2003.netlify.app/");
            _statusBar.SetStatus("🏠 Going home...", "#00FF88");
            // Navigation completed event will set to Ready.
        }

        private void UpdateButtons()
        {
            _backButton.IsEnabled = _webViewManager.CanGoBack;
        }
    }
}