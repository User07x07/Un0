using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Un0.Services; // your services namespace

namespace Un0
{
    public partial class MainWindow : Window
    {
        // ----- Services -----
        private WebViewManager _webViewManager;
        private StatusBarService _statusBar;
        private ProtectionStatusService _protectionStatus;
        private AdBlockerService _adBlocker;
        private VolumeBoostService _volumeBoost;
        private UpdateService _updateService;
        private FullscreenService _fullscreenService;
        private NavigationService _navigationService;

        // ----- UI state (still in main form) -----
        private bool _isLoaded = false;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services (inject UI dependencies)
            _statusBar = new StatusBarService(StatusLabel);
            _protectionStatus = new ProtectionStatusService(ProtectionIcon, ProtectionLabel);
            _webViewManager = new WebViewManager(WebView, WebViewContainer, LoadProgressBar);
            _adBlocker = new AdBlockerService(_webViewManager, _protectionStatus);
            _volumeBoost = new VolumeBoostService(_webViewManager, _statusBar);
            _updateService = new UpdateService(_statusBar);
            _fullscreenService = new FullscreenService(
                this,
                TitleBarBorder,
                StatusBarBorder,
                FullscreenControls,
                _statusBar,
                _webViewManager
            );
            _navigationService = new NavigationService(_webViewManager, BackButton, _statusBar);

            // Wire events from services to UI updates
            _webViewManager.ContainsFullscreenElementChanged += (s, isFullscreen) =>
            {
                if (isFullscreen)
                    _fullscreenService.EnterFullscreen();
                else
                    _fullscreenService.ExitFullscreen();
            };

            _webViewManager.WebMessageReceived += OnWebMessageReceived;

            _updateService.UpdateAvailabilityChanged += (s, hasUpdate) =>
            {
                ShowUpdateWarning(hasUpdate);
            };

            this.Loaded += MainWindow_Loaded;
            FullscreenControls.Visibility = Visibility.Collapsed;
            UpdateVersionDisplay();
        }

        // ----- Initialization -----
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded) return;
            _isLoaded = true;

            try
            {
                string userDataFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Un0_Cache"
                );

                await _webViewManager.InitializeAsync(userDataFolder);

                // Inject scripts
                await _webViewManager.InjectScriptAsync(_adBlocker.GetAdBlockScript());
                await _webViewManager.InjectScriptAsync(_volumeBoost.GetVolumeBoostScript());

                // Navigate to home
                _webViewManager.NavigateTo("https://mainframe2003.netlify.app/");
                _statusBar.SetStatus("Ready", "#666666");

                // Update protection status
                _protectionStatus.UpdateProtectionStatus(0);

                // Check for updates
                await _updateService.CheckForUpdatesOnLoad();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing: {ex.Message}\n\n" +
                    "Please install WebView2 Runtime from:\n" +
                    "https://developer.microsoft.com/en-us/microsoft-edge/webview2/",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ----- WebMessageReceived handler (for messages not handled by services) -----
        private void OnWebMessageReceived(object sender, WebMessageEventArgs e)
        {
            string message = e.Message;

            if (message.Contains("video_playing"))
            {
                _statusBar.SetStatus("▶ Video playing", "#00FF88");
            }
            else if (message.Contains("video_fullscreen_enter"))
            {
                _fullscreenService.EnterFullscreen();
            }
            else if (message.Contains("video_fullscreen_exit"))
            {
                _fullscreenService.ExitFullscreen();
            }
            else if (message.Contains("google_auth_attempt"))
            {
                _statusBar.SetStatus("🔐 Authenticating...", "#00FF88");
            }
            else if (message.Contains("google_auth_complete"))
            {
                _statusBar.SetStatus("Ready", "#666666");
            }
            else if (message.Contains("user_sign_out"))
            {
                _adBlocker.ResetCounter();
                _statusBar.SetStatus("Signed out", "#FF6B6B");
                Task.Delay(3000).ContinueWith(_ =>
                    Dispatcher.Invoke(() => _statusBar.SetStatus("Ready", "#666666"))
                );
            }
            // Additional messages are handled by the individual services (ad_blocked, volume_boost, etc.)
        }

        // ----- UI update helpers (still in main form) -----
        private void UpdateVersionDisplay()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    var versionString = $"{version.Major}.{version.Minor}.{version.Build}";
                    VersionText.Text = $" v{versionString}";
                    Debug.WriteLine($"Version displayed: v{versionString}");

                    if (App.IsLatestVersion)
                    {
                        LatestLabel.Visibility = Visibility.Visible;
                        LatestLabel.Text = " ✓ Latest";
                        LatestLabel.Foreground = (Brush)new BrushConverter().ConvertFromString("#90EE90");
                    }
                    else
                    {
                        LatestLabel.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    VersionText.Text = " v1.0.0";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get version: {ex.Message}");
                VersionText.Text = " v1.0.0";
            }
        }

        private void ShowUpdateWarning(bool show)
        {
            Dispatcher.Invoke(() =>
            {
                if (show)
                {
                    UpdateWarningLabel.Visibility = Visibility.Visible;
                    FullscreenHint.Visibility = Visibility.Collapsed;
                    _statusBar.SetStatus(" Update available!", "#FF4444");

                    WebView.IsEnabled = false;
                    WebViewBorder.Opacity = 0.5;

                    ShowWebViewOverlay(true);
                }
                else
                {
                    UpdateWarningLabel.Visibility = Visibility.Collapsed;
                    FullscreenHint.Visibility = Visibility.Visible;
                    _statusBar.SetStatus(" Ready", "#666666");

                    WebView.IsEnabled = true;
                    WebViewBorder.Opacity = 1.0;

                    ShowWebViewOverlay(false);
                }
            });
        }

        private void ShowWebViewOverlay(bool show)
        {
            Dispatcher.Invoke(() =>
            {
                var overlay = WebViewContainer.FindName("UpdateOverlay") as Grid;

                if (show)
                {
                    if (overlay == null)
                    {
                        overlay = new Grid();
                        overlay.Name = "UpdateOverlay";
                        overlay.Background = new SolidColorBrush(Color.FromArgb(200, 10, 10, 10));
                        overlay.HorizontalAlignment = HorizontalAlignment.Stretch;
                        overlay.VerticalAlignment = VerticalAlignment.Stretch;

                        var stackPanel = new StackPanel
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        var iconText = new TextBlock
                        {
                            Text = "⚠️",
                            Foreground = (Brush)new BrushConverter().ConvertFromString("#FF4444"),
                            FontSize = 48,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        stackPanel.Children.Add(iconText);

                        var messageText = new TextBlock
                        {
                            Text = "UPDATE REQUIRED",
                            Foreground = (Brush)new BrushConverter().ConvertFromString("#FF4444"),
                            FontSize = 28,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        stackPanel.Children.Add(messageText);

                        var subMessageText = new TextBlock
                        {
                            Text = "Please update to the latest version to continue using Un0.",
                            Foreground = (Brush)new BrushConverter().ConvertFromString("#CCCCCC"),
                            FontSize = 16,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 20),
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center,
                            MaxWidth = 500
                        };
                        stackPanel.Children.Add(subMessageText);

                        var updateButton = new Button
                        {
                            Content = "Check for Updates",
                            Width = 200,
                            Height = 45,
                            FontSize = 14,
                            FontWeight = FontWeights.SemiBold,
                            Background = (Brush)new BrushConverter().ConvertFromString("#00FF88"),
                            Foreground = (Brush)new BrushConverter().ConvertFromString("#0A0A0A"),
                            BorderThickness = new Thickness(0),
                            Cursor = Cursors.Hand,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        updateButton.Click += UpdateButton_Click;
                        stackPanel.Children.Add(updateButton);

                        overlay.Children.Add(stackPanel);
                        WebViewContainer.Children.Add(overlay);
                    }
                    overlay.Visibility = Visibility.Visible;
                }
                else
                {
                    if (overlay != null)
                        overlay.Visibility = Visibility.Collapsed;
                }
            });
        }

        // ----- Navigation button handlers (delegated to NavigationService) -----
        private void BackButton_Click(object sender, RoutedEventArgs e) => _navigationService.GoBack();
        private void RefreshButton_Click(object sender, RoutedEventArgs e) => _navigationService.Refresh();
        private void HomeButton_Click(object sender, RoutedEventArgs e) => _navigationService.GoHome();

        // ----- Volume boost handler (delegated) -----
        private async void VolumeBoostButton_Click(object sender, RoutedEventArgs e)
        {
            await _volumeBoost.ToggleBoost();
            // Update button appearance based on state
            if (_volumeBoost.IsBoosted)
            {
                VolumeBoostButton.Content = "🔊";
                VolumeBoostButton.Background = (Brush)new BrushConverter().ConvertFromString("#00FF88");
                VolumeBoostButton.Foreground = (Brush)new BrushConverter().ConvertFromString("#0A0A0A");
            }
            else
            {
                VolumeBoostButton.Content = "🔊";
                VolumeBoostButton.Background = (Brush)new BrushConverter().ConvertFromString("#222222");
                VolumeBoostButton.Foreground = (Brush)new BrushConverter().ConvertFromString("#888888");
            }
        }

        // ----- Fullscreen control handlers -----
        private void ExitFullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            _fullscreenService.ExitFullscreenAndCloseVideo();
        }

        private void CloseVideoButton_Click(object sender, RoutedEventArgs e)
        {
            _fullscreenService.ExitFullscreenAndCloseVideo();
        }

        // ----- Update handler (delegated) -----
        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await _updateService.ManualUpdateCheck();
        }

        // ----- Title bar controls -----
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_fullscreenService.IsFullscreenMode)
            {
                if (e.ClickCount == 2)
                    MaximizeButton_Click(sender, e);
                else
                    this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_fullscreenService.IsFullscreenMode)
                this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_fullscreenService.IsFullscreenMode)
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

        // ----- Hover effects (can remain here) -----
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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try { WebView?.Dispose(); } catch { }
            base.OnClosing(e);
        }
    }
}