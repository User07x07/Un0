using System.Windows;
using System.Windows.Controls;

namespace Un0.Services
{
    public class FullscreenService
    {
        private Window _window;
        private Border _titleBarBorder;
        private Border _statusBarBorder;
        private UIElement _fullscreenControls;   // Changed to UIElement
        private StatusBarService _statusBarService;
        private WebViewManager _webViewManager;

        public bool IsFullscreenMode { get; private set; }
        public bool IsVideoFullscreen { get; private set; }

        public FullscreenService(
            Window window,
            Border titleBar,
            Border statusBarBorder,        // renamed from 'statusBar'
            UIElement fullscreenControls,  // now UIElement
            StatusBarService statusBarService,
            WebViewManager webViewManager)
        {
            _window = window;
            _titleBarBorder = titleBar;
            _statusBarBorder = statusBarBorder;
            _fullscreenControls = fullscreenControls;
            _statusBarService = statusBarService;
            _webViewManager = webViewManager;
        }

        public void EnterFullscreen()
        {
            if (IsFullscreenMode) return;
            IsFullscreenMode = true;
            IsVideoFullscreen = true;

            _titleBarBorder.Visibility = Visibility.Collapsed;
            _statusBarBorder.Visibility = Visibility.Collapsed;
            _fullscreenControls.Visibility = Visibility.Visible;

            _window.WindowStyle = WindowStyle.None;
            _window.WindowState = WindowState.Maximized;
            _window.Topmost = true;

            _statusBarService.SetStatus("▶ Fullscreen", "#00FF88");
        }

        public void ExitFullscreen()
        {
            if (!IsFullscreenMode) return;
            IsFullscreenMode = false;
            IsVideoFullscreen = false;

            _titleBarBorder.Visibility = Visibility.Visible;
            _statusBarBorder.Visibility = Visibility.Visible;
            _fullscreenControls.Visibility = Visibility.Collapsed;

            _window.WindowStyle = WindowStyle.None;
            _window.WindowState = WindowState.Normal;
            _window.Topmost = false;

            _statusBarService.SetStatus("Ready", "#666666");
        }

        public async void ExitFullscreenAndCloseVideo()
        {
            ExitFullscreen();
            await _webViewManager.ExecuteScriptAsync(@"
                const videos = document.getElementsByTagName('video');
                for (let video of videos) video.pause();
                const iframes = document.getElementsByTagName('iframe');
                for (let iframe of iframes) {
                    if (iframe.src.includes('youtube') || iframe.src.includes('vimeo')) {
                        iframe.src = '';
                    }
                }
            ");
        }
    }
}