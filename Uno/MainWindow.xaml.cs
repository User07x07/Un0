using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Un0
{
    public partial class MainWindow : Window
    {
        private int adBlockCounter = 0;
        private string currentUrl = "";
        private bool isAuthenticated = false;
        private bool isVideoFullscreen = false;
        private bool isFullscreenMode = false;
        private bool isVolumeBoosted = false;

        private string adBlockScript = @"
            (function() {
                let adCount = 0;

                function incrementAdCounter() {
                    adCount++;
                    try {
                        window.chrome.webview.postMessage('ad_blocked_' + adCount);
                    } catch(e) {}
                }

                const originalOpen = window.open;
                window.open = function(url, name, features) {
                    if (url && (url.includes('accounts.google.com') || 
                                url.includes('googleapis.com') || 
                                url.includes('google.com'))) {
                        return originalOpen.call(window, url, name, features);
                    }
                    incrementAdCounter();
                    return null;
                };

                function removeAds() {
                    let removed = 0;
                    const selectors = [
                        '.ytp-ad-module', '.ytp-ad-player-overlay', '.video-ads',
                        '.ad-container', '.ad-showing', '[id*=""google_ads""]',
                        '[id*=""ad-container""]', '[id*=""popup""]', '[id*=""modal""]',
                        '[id*=""overlay""]', '.advertisement', '.adsbygoogle',
                        '[class*=""ad-""]', '[class*=""_ad_""]', '[class*=""popup""]',
                        '[class*=""modal""]', '[class*=""overlay""]', '.game_area_purchase',
                        '.game_purchase_action', '.game_area_bubble',
                        '[class*=""newsletter""]', '[id*=""newsletter""]'
                    ];
                    selectors.forEach(selector => {
                        document.querySelectorAll(selector).forEach(el => {
                            if (el.closest('.upload-area') || 
                                el.closest('.drop-zone') ||
                                el.closest('[class*=""upload""]') ||
                                el.closest('[class*=""drop""]') ||
                                el.closest('input[type=""file""]') ||
                                el.id && el.id.includes('upload') ||
                                el.id && el.id.includes('drop')) {
                                return;
                            }
                            el.remove();
                            removed++;
                        });
                    });

                    document.querySelectorAll('input[type=""file""]').forEach(el => {});

                    if (removed > 0) {
                        for (let i = 0; i < removed; i++) {
                            incrementAdCounter();
                        }
                    }
                }

                const observer = new MutationObserver(function(mutations) {
                    removeAds();
                    mutations.forEach(function(mutation) {
                        mutation.addedNodes.forEach(function(node) {
                            if (node.nodeType === 1) {
                                const el = node;
                                const id = (el.id || '').toLowerCase();
                                const className = (el.className || '').toLowerCase();
                                
                                if (id.includes('upload') || id.includes('drop') || 
                                    className.includes('upload') || className.includes('drop') ||
                                    id.includes('file') || className.includes('file')) {
                                    return;
                                }
                                
                                if (id.includes('popup') || id.includes('modal') || 
                                    className.includes('popup') || className.includes('modal')) {
                                    if (!el.querySelector('input[type=""file""]') && 
                                        !el.querySelector('.upload-area') &&
                                        !el.querySelector('.drop-zone')) {
                                        el.remove();
                                        incrementAdCounter();
                                    }
                                }
                            }
                        });
                    });
                });

                if (document.body) {
                    observer.observe(document.body, {
                        childList: true,
                        subtree: true,
                        attributes: true,
                        attributeFilter: ['style', 'class', 'id']
                    });
                }

                window.addEventListener('load', function() {
                    setTimeout(removeAds, 500);
                    setTimeout(removeAds, 1500);
                    setTimeout(removeAds, 3000);
                    setInterval(removeAds, 3000);
                });

                function detectVideoPlaying() {
                    const videos = document.getElementsByTagName('video');
                    for (let video of videos) {
                        if (!video.paused && !video.ended && video.readyState > 2) {
                            return true;
                        }
                    }
                    return false;
                }

                setInterval(function() {
                    if (detectVideoPlaying()) {
                        window.chrome.webview.postMessage('video_playing');
                    }
                }, 2000);

                function handleFullscreenChange() {
                    const isFullscreen = !!(document.fullscreenElement || 
                                           document.webkitFullscreenElement || 
                                           document.mozFullScreenElement);
                    if (isFullscreen) {
                        window.chrome.webview.postMessage('video_fullscreen_enter');
                    } else {
                        window.chrome.webview.postMessage('video_fullscreen_exit');
                    }
                }

                document.addEventListener('fullscreenchange', handleFullscreenChange);
                document.addEventListener('webkitfullscreenchange', handleFullscreenChange);
                document.addEventListener('mozfullscreenchange', handleFullscreenChange);

                setInterval(function() {
                    const fullscreenEl = document.fullscreenElement || 
                                        document.webkitFullscreenElement || 
                                        document.mozFullScreenElement;
                    if (fullscreenEl && (fullscreenEl.tagName === 'VIDEO' || 
                                        fullscreenEl.querySelector('video'))) {
                        window.chrome.webview.postMessage('video_fullscreen_enter');
                    }
                }, 1000);

                document.addEventListener('dragover', function(e) {
                    e.preventDefault();
                });

                document.addEventListener('drop', function(e) {
                    e.preventDefault();
                    const files = e.dataTransfer.files;
                    if (files.length > 0) {
                        const inputs = document.querySelectorAll('input[type=""file""]');
                        for (let input of inputs) {
                            const dt = new DataTransfer();
                            for (let file of files) {
                                dt.items.add(file);
                            }
                            input.files = dt.files;
                            input.dispatchEvent(new Event('change'));
                        }
                    }
                });

                document.addEventListener('click', function(e) {
                    const target = e.target;
                    if (target && target.type === 'file') {
                        target.click();
                    }
                    if (target && (target.className.includes('upload') || 
                                   target.className.includes('drop') ||
                                   target.id.includes('upload') ||
                                   target.id.includes('drop'))) {
                        const fileInput = target.querySelector('input[type=""file""]') || 
                                         document.querySelector('input[type=""file""]');
                        if (fileInput) {
                            fileInput.click();
                        }
                    }
                });

                console.log('AdBlock active - File upload support enabled');
            })();
        ";

        private string volumeBoostScript = @"
            (function() {
                let isBoosted = false;
                let originalVolume = 1.0;
                
                function boostVolume() {
                    const videos = document.getElementsByTagName('video');
                    let boosted = false;
                    
                    if (!isBoosted) {
                        for (let video of videos) {
                            if (video.volume !== undefined) {
                                if (video.volume > 0.1) {
                                    originalVolume = video.volume;
                                }
                                video.volume = Math.min(4.0, video.volume * );
                                boosted = true;
                            }
                        }
                        isBoosted = true;
                        window.chrome.webview.postMessage('volume_boosted_on');
                    } else {
                        for (let video of videos) {
                            if (video.volume !== undefined) {
                                video.volume = Math.min(1.0, originalVolume);
                                boosted = true;
                            }
                        }
                        isBoosted = false;
                        window.chrome.webview.postMessage('volume_boosted_off');
                    }
                    
                    return boosted;
                }
                
                window.addEventListener('message', function(event) {
                    if (event.data === 'toggle_volume_boost') {
                        boostVolume();
                    }
                });
                
                console.log('Volume Boost script loaded');
            })();
        ";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            FullscreenControls.Visibility = Visibility.Collapsed;
            UpdateVersionDisplay();
        }

        private void UpdateVersionDisplay()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    var versionString = $"{version.Major}.{version.Minor}.{version.Build}";
                    VersionText.Text = $" v{versionString}";
                    UpdateService.SetVersion(versionString);
                    Debug.WriteLine($"Version displayed: v{versionString}");
                }
                else
                {
                    VersionText.Text = " v1.2.1";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get version: {ex.Message}");
                VersionText.Text = " v1.2.1";
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateButton.IsEnabled = false;
                UpdateButton.Foreground = (Brush)new BrushConverter().ConvertFromString("#FFD93D");
                StatusLabel.Text = " Checking for updates...";
                UpdateStatusBarColor("#FFD93D");

                var updateService = new UpdateService();
                var hasUpdate = await updateService.CheckForUpdatesAsync();

                if (hasUpdate)
                {
                    var result = MessageBox.Show(
                        "A new version is available!\n\n" +
                        "Would you like to download and install it now?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        await updateService.DownloadAndInstallUpdateAsync();
                    }
                    else
                    {
                        StatusLabel.Text = " Update cancelled";
                        UpdateStatusBarColor("#666666");
                        await Task.Delay(1500);
                        StatusLabel.Text = "Ready";
                        UpdateStatusBarColor("#666666");
                    }
                }
                else
                {
                    StatusLabel.Text = " ✓ You have the latest version";
                    UpdateStatusBarColor("#00FF88");
                    await Task.Delay(3000);
                    StatusLabel.Text = "Ready";
                    UpdateStatusBarColor("#666666");
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = " Update check failed";
                UpdateStatusBarColor("#FF4444");
                await Task.Delay(2000);
                StatusLabel.Text = "Ready";
                UpdateStatusBarColor("#666666");
            }
            finally
            {
                UpdateButton.IsEnabled = true;
                UpdateButton.Foreground = (Brush)new BrushConverter().ConvertFromString("#00FF88");
            }
        }

        private async void VolumeBoostButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WebView?.CoreWebView2 != null)
                {
                    isVolumeBoosted = !isVolumeBoosted;

                    await WebView.CoreWebView2.ExecuteScriptAsync(@"
                        window.postMessage('toggle_volume_boost');
                    ");

                    if (isVolumeBoosted)
                    {
                        VolumeBoostButton.Content = "🔊";
                        VolumeBoostButton.Background = (Brush)new BrushConverter().ConvertFromString("#00FF88");
                        VolumeBoostButton.Foreground = (Brush)new BrushConverter().ConvertFromString("#0A0A0A");
                        StatusLabel.Text = "🔊 Volume Boost ON (400%)";
                        UpdateStatusBarColor("#00FF88");

                        await Task.Delay(2000);
                        if (!StatusLabel.Text.Contains("Video"))
                        {
                            StatusLabel.Text = "Ready";
                            UpdateStatusBarColor("#666666");
                        }
                    }
                    else
                    {
                        VolumeBoostButton.Content = "🔊";
                        VolumeBoostButton.Background = (Brush)new BrushConverter().ConvertFromString("#222222");
                        VolumeBoostButton.Foreground = (Brush)new BrushConverter().ConvertFromString("#888888");
                        StatusLabel.Text = "🔊 Volume Boost OFF";
                        UpdateStatusBarColor("#666666");

                        await Task.Delay(1500);
                        if (!StatusLabel.Text.Contains("Video"))
                        {
                            StatusLabel.Text = "Ready";
                            UpdateStatusBarColor("#666666");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Volume Boost error: {ex.Message}");
                StatusLabel.Text = "Volume boost failed";
                UpdateStatusBarColor("#FF4444");
                await Task.Delay(1500);
                StatusLabel.Text = "Ready";
                UpdateStatusBarColor("#666666");
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateVersionDisplay();

                string userDataFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Un0_Cache"
                );

                var options = new CoreWebView2EnvironmentOptions();
                var env = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: userDataFolder,
                    options: options
                );

                await WebView.EnsureCoreWebView2Async(env);

                // Inject volume boost script
                await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(volumeBoostScript);

                WebView.CoreWebView2.ContainsFullScreenElementChanged += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (WebView.CoreWebView2.ContainsFullScreenElement)
                        {
                            EnterFullscreen();
                        }
                        else
                        {
                            ExitFullscreen();
                        }
                    });
                };

                WebView.CoreWebView2.NewWindowRequested += (s, args) =>
                {
                    if (args.Uri != null &&
                        (args.Uri.Contains("accounts.google.com") ||
                         args.Uri.Contains("googleapis.com") ||
                         args.Uri.Contains("google.com")))
                    {
                        args.Handled = false;
                        Dispatcher.Invoke(() => ShowAuthStatus(true));
                        return;
                    }

                    if (args.Uri != null &&
                        (args.Uri.Contains("upload") ||
                         args.Uri.Contains("file") ||
                         args.Uri.Contains("drop")))
                    {
                        args.Handled = false;
                        return;
                    }

                    args.Handled = true;
                    adBlockCounter++;
                    Dispatcher.Invoke(() => UpdateProtectionStatus());
                };

                WebView.CoreWebView2.WebMessageReceived += (s, args) =>
                {
                    string message = args.WebMessageAsJson;
                    if (message.Contains("ad_blocked_"))
                    {
                        adBlockCounter++;
                        Dispatcher.Invoke(() => UpdateProtectionStatus());
                    }
                    else if (message.Contains("video_playing"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StatusLabel.Text = "▶ Video playing";
                            UpdateStatusBarColor("#00FF88");
                        });
                    }
                    else if (message.Contains("video_fullscreen_enter"))
                    {
                        Dispatcher.Invoke(() => EnterFullscreen());
                    }
                    else if (message.Contains("video_fullscreen_exit"))
                    {
                        Dispatcher.Invoke(() => ExitFullscreen());
                    }
                    else if (message.Contains("volume_boosted_on"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StatusLabel.Text = "🔊 Volume Boost ON (400%)";
                            UpdateStatusBarColor("#00FF88");
                        });
                    }
                    else if (message.Contains("volume_boosted_off"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StatusLabel.Text = "🔊 Volume Boost OFF";
                            UpdateStatusBarColor("#666666");
                        });
                    }
                    else if (message.Contains("google_auth_attempt"))
                    {
                        Dispatcher.Invoke(() => ShowAuthStatus(true));
                    }
                    else if (message.Contains("google_auth_complete"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ShowAuthStatus(false);
                            StatusLabel.Text = "Ready";
                            UpdateStatusBarColor("#666666");
                        });
                    }
                    else if (message.Contains("user_sign_out"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            isAuthenticated = false;
                            adBlockCounter = 0;
                            ShowAuthStatus(false);
                            UpdateProtectionStatus();
                            StatusLabel.Text = "Signed out";
                            UpdateStatusBarColor("#FF6B6B");
                            Task.Delay(3000).ContinueWith(_ =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    StatusLabel.Text = "Ready";
                                    UpdateStatusBarColor("#666666");
                                });
                            });
                        });
                    }
                };

                WebView.CoreWebView2.NavigationStarting += (s, args) =>
                {
                    currentUrl = args.Uri;
                    Dispatcher.Invoke(() =>
                    {
                        if (args.Uri != null &&
                            (args.Uri.Contains("accounts.google.com") ||
                             args.Uri.Contains("googleapis.com")))
                        {
                            ShowAuthStatus(true);
                            LoadProgressBar.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            LoadProgressBar.Visibility = Visibility.Visible;
                            StatusLabel.Text = "Loading...";
                        }
                        UpdateNavigationButtons();
                    });
                };

                WebView.CoreWebView2.NavigationCompleted += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        LoadProgressBar.Visibility = Visibility.Collapsed;
                        if (!args.IsSuccess)
                        {
                            StatusLabel.Text = "Connection error";
                            UpdateStatusBarColor("#FF4444");
                        }
                        else
                        {
                            StatusLabel.Text = "Ready";
                            UpdateStatusBarColor("#666666");
                            InjectAdBlock();
                        }
                        UpdateNavigationButtons();
                    });
                };

                WebView.CoreWebView2.DocumentTitleChanged += (s, args) =>
                {
                    Dispatcher.Invoke(() => { });
                };

                await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(adBlockScript);
                WebView.CoreWebView2.Navigate("https://mainframe2003.netlify.app/");
                StatusLabel.Text = "Ready";
                UpdateProtectionStatus();
                UpdateNavigationButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing: {ex.Message}\n\n" +
                    "Please install WebView2 Runtime from:\n" +
                    "https://developer.microsoft.com/en-us/microsoft-edge/webview2/",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnterFullscreen()
        {
            if (!isFullscreenMode)
            {
                isFullscreenMode = true;
                isVideoFullscreen = true;

                TitleBarBorder.Visibility = Visibility.Collapsed;
                StatusBarBorder.Visibility = Visibility.Collapsed;
                FullscreenControls.Visibility = Visibility.Visible;

                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                this.Topmost = true;

                WebViewContainer.Margin = new Thickness(0);
                WebView.Margin = new Thickness(0);

                StatusLabel.Text = "▶ Fullscreen";
                UpdateStatusBarColor("#00FF88");
            }
        }

        private void ExitFullscreen()
        {
            if (isFullscreenMode || isVideoFullscreen)
            {
                isFullscreenMode = false;
                isVideoFullscreen = false;

                TitleBarBorder.Visibility = Visibility.Visible;
                StatusBarBorder.Visibility = Visibility.Visible;
                FullscreenControls.Visibility = Visibility.Collapsed;

                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Normal;
                this.Topmost = false;

                WebViewContainer.Margin = new Thickness(0);
                WebView.Margin = new Thickness(0);

                StatusLabel.Text = "Ready";
                UpdateStatusBarColor("#666666");
            }
        }

        private void ExitFullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            ExitFullscreen();
            try
            {
                WebView.CoreWebView2?.ExecuteScriptAsync(@"
                    if (document.fullscreenElement) {
                        document.exitFullscreen();
                    } else if (document.webkitFullscreenElement) {
                        document.webkitExitFullscreen();
                    } else if (document.mozFullScreenElement) {
                        document.mozCancelFullScreen();
                    }
                ");
            }
            catch { }
        }

        private void CloseVideoButton_Click(object sender, RoutedEventArgs e)
        {
            ExitFullscreen();
            try
            {
                WebView.CoreWebView2?.ExecuteScriptAsync(@"
                    const videos = document.getElementsByTagName('video');
                    for (let video of videos) {
                        video.pause();
                    }
                    const iframes = document.getElementsByTagName('iframe');
                    for (let iframe of iframes) {
                        if (iframe.src.includes('youtube') || iframe.src.includes('vimeo')) {
                            iframe.src = '';
                        }
                    }
                ");
            }
            catch { }
        }

        private void ShowAuthStatus(bool show)
        {
            Dispatcher.Invoke(() =>
            {
                if (show && !isAuthenticated)
                {
                    StatusLabel.Text = "🔐 Authenticating...";
                    UpdateStatusBarColor("#00FF88");
                }
                else
                {
                    if (!StatusLabel.Text.Contains("Video") && !StatusLabel.Text.Contains("Fullscreen"))
                    {
                        StatusLabel.Text = "Ready";
                        UpdateStatusBarColor("#666666");
                    }
                }
            });
        }

        private void InjectAdBlock()
        {
            try
            {
                WebView.CoreWebView2?.ExecuteScriptAsync(adBlockScript);
            }
            catch { }
        }

        private void UpdateProtectionStatus()
        {
            Dispatcher.Invoke(() =>
            {
                ProtectionIcon.Foreground = (Brush)new BrushConverter().ConvertFromString("#00FF88");
                ProtectionLabel.Text = $"Protected: {adBlockCounter}";
                ProtectionLabel.Foreground = (Brush)new BrushConverter().ConvertFromString("#00FF88");

                if (adBlockCounter > 20)
                {
                    ProtectionLabel.Foreground = (Brush)new BrushConverter().ConvertFromString("#FF6B6B");
                    ProtectionIcon.Foreground = (Brush)new BrushConverter().ConvertFromString("#FF6B6B");
                }
                else if (adBlockCounter > 10)
                {
                    ProtectionLabel.Foreground = (Brush)new BrushConverter().ConvertFromString("#FFD93D");
                    ProtectionIcon.Foreground = (Brush)new BrushConverter().ConvertFromString("#FFD93D");
                }
            });
        }

        private void UpdateStatusBarColor(string colorHex)
        {
            try
            {
                var converter = new System.Windows.Media.BrushConverter();
                StatusLabel.Foreground = (Brush)converter.ConvertFromString(colorHex);
            }
            catch { }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebView?.CoreWebView2 != null && WebView.CoreWebView2.CanGoBack)
            {
                WebView.CoreWebView2.GoBack();
                StatusLabel.Text = "Going back...";
                UpdateStatusBarColor("#00FF88");

                Task.Delay(1500).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (!StatusLabel.Text.Contains("Video"))
                        {
                            StatusLabel.Text = "Ready";
                            UpdateStatusBarColor("#666666");
                        }
                    });
                });
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebView?.CoreWebView2 != null)
            {
                WebView.CoreWebView2.Reload();
                StatusLabel.Text = "🔄 Refreshing...";
                UpdateStatusBarColor("#FFD93D");

                RefreshButton.Opacity = 0.5;

                Task.Delay(1500).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        RefreshButton.Opacity = 1.0;
                        if (!StatusLabel.Text.Contains("Video"))
                        {
                            StatusLabel.Text = "Ready";
                            UpdateStatusBarColor("#666666");
                        }
                    });
                });
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebView?.CoreWebView2 != null)
            {
                WebView.CoreWebView2.Navigate("https://mainframe2003.netlify.app/");
                StatusLabel.Text = "🏠 Going home...";
                UpdateStatusBarColor("#00FF88");

                Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (!StatusLabel.Text.Contains("Video"))
                        {
                            StatusLabel.Text = "Ready";
                            UpdateStatusBarColor("#666666");
                        }
                    });
                });
            }
        }

        private void UpdateNavigationButtons()
        {
            if (WebView?.CoreWebView2 != null)
            {
                BackButton.IsEnabled = WebView.CoreWebView2.CanGoBack;
                BackButton.Opacity = BackButton.IsEnabled ? 1.0 : 0.5;
                BackButton.Foreground = BackButton.IsEnabled ?
                    (Brush)new BrushConverter().ConvertFromString("#FFFFFF") :
                    (Brush)new BrushConverter().ConvertFromString("#555555");
            }
        }

        private void NavButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn && btn.IsEnabled)
            {
                btn.Background = (Brush)new BrushConverter().ConvertFromString("#333333");
                btn.Foreground = (Brush)new BrushConverter().ConvertFromString("#FFFFFF");
            }
        }

        private void NavButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                if (btn.IsEnabled)
                {
                    btn.Background = (Brush)new BrushConverter().ConvertFromString("#222222");
                    btn.Foreground = (Brush)new BrushConverter().ConvertFromString("#888888");
                }
                else
                {
                    btn.Background = (Brush)new BrushConverter().ConvertFromString("#222222");
                    btn.Foreground = (Brush)new BrushConverter().ConvertFromString("#555555");
                }
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isFullscreenMode)
            {
                if (e.ClickCount == 2)
                {
                    MaximizeButton_Click(sender, e);
                }
                else
                {
                    this.DragMove();
                }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isFullscreenMode)
                this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isFullscreenMode)
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.WindowState = WindowState.Maximized;
                    MaximizeButton.Content = "☒";
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    MaximizeButton.Content = "☐";
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try { WebView?.Dispose(); } catch { }
            Application.Current.Shutdown();
        }

        private void CloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            CloseButton.Background = (Brush)new BrushConverter().ConvertFromString("#E81123");
            CloseButton.Foreground = Brushes.White;
        }

        private void CloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseButton.Background = Brushes.Transparent;
            CloseButton.Foreground = (Brush)new BrushConverter().ConvertFromString("#AAAAAA");
        }

        private void ControlButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn && btn != CloseButton)
            {
                btn.Background = (Brush)new BrushConverter().ConvertFromString("#333333");
                btn.Foreground = Brushes.White;
            }
        }

        private void ControlButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn && btn != CloseButton)
            {
                btn.Background = Brushes.Transparent;
                btn.Foreground = (Brush)new BrushConverter().ConvertFromString("#AAAAAA");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try { WebView?.Dispose(); } catch { }
            base.OnClosing(e);
        }
    }
}