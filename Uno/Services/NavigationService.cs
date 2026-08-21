using Microsoft.Web.WebView2.Core;
using System.Windows.Controls;

namespace Un0.Services
{
    public class NavigationService
    {
        private WebViewManager _webViewManager;
        private Button _backButton;
        private StatusBarService _statusBar;

        public NavigationService(WebViewManager webViewManager, Button backButton, StatusBarService statusBar)
        {
            _webViewManager = webViewManager;
            _backButton = backButton;
            _statusBar = statusBar;
            _webViewManager.NavigationCompleted += (s, e) => UpdateButtons();
            _webViewManager.NavigationStarting += (s, e) => UpdateButtons();
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
        }

        public void GoHome()
        {
            _webViewManager.NavigateTo("https://mainframe2003.netlify.app/");
            _statusBar.SetStatus("🏠 Going home...", "#00FF88");
        }

        private void UpdateButtons()
        {
            _backButton.IsEnabled = _webViewManager.CanGoBack;
        }
    }
}