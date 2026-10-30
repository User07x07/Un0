using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Un0.Services
{
    // Simple event args wrapper for web messages
    public class WebMessageEventArgs : EventArgs
    {
        public string Message { get; }
        public WebMessageEventArgs(string message) => Message = message;
    }

    public class WebViewManager
    {
        private WebView2 _webView;
        private Panel _webViewContainer;
        private ProgressBar _loadProgressBar;

        public CoreWebView2 CoreWebView2 => _webView?.CoreWebView2;
        public bool CanGoBack => CoreWebView2?.CanGoBack ?? false;

        // Events – using built‑in args for navigation, custom args for web messages
        public event EventHandler<CoreWebView2NavigationStartingEventArgs> NavigationStarting;
        public event EventHandler<CoreWebView2NavigationCompletedEventArgs> NavigationCompleted;
        public event EventHandler<WebMessageEventArgs> WebMessageReceived;   // Changed to custom
        public event EventHandler<bool> ContainsFullscreenElementChanged;
        public event EventHandler RequestBlocked;

        // Set by the ad blocker: returns true when a sub-request (script, image, video ad, tracker) should be blocked
        public Func<Uri, bool> ShouldBlockRequest { get; set; }

        public WebViewManager(WebView2 webView, Panel container, ProgressBar progressBar)
        {
            _webView = webView;
            _webViewContainer = container;
            _loadProgressBar = progressBar;
        }

        public async Task InitializeAsync(string userDataFolder)
        {
            var options = new CoreWebView2EnvironmentOptions();
            // Use the overload that takes (browserExecutableFolder, userDataFolder, options)
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
            await _webView.EnsureCoreWebView2Async(env);

            // Right‑click is off and developer tools cannot be opened:
            // no default context menu, no DevTools, and no F12 / Ctrl+Shift+I shortcuts.
            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

            // Belt and braces: if a page still raises a context menu request, swallow it.
            _webView.CoreWebView2.ContextMenuRequested += (s, e) => e.Handled = true;

            // Stop the WPF host from showing its own right‑click menu over the view
            _webView.ContextMenuOpening += (s, e) => e.Handled = true;

            // Subscribe to events
            _webView.CoreWebView2.ContainsFullScreenElementChanged += (s, e) =>
                ContainsFullscreenElementChanged?.Invoke(this, _webView.CoreWebView2.ContainsFullScreenElement);

            _webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;

            // WebMessageReceived: wrap the message in our custom event args
            _webView.CoreWebView2.WebMessageReceived += (s, e) =>
            {
                // Try to get plain string; if not available, use the JSON representation
                string message = e.TryGetWebMessageAsString();
                if (message == null)
                {
                    // Fallback: get as JSON (will include quotes)
                    message = e.WebMessageAsJson;
                    // Optionally trim surrounding quotes if needed
                    if (message.StartsWith("\"") && message.EndsWith("\""))
                        message = message.Substring(1, message.Length - 2);
                }
                WebMessageReceived?.Invoke(this, new WebMessageEventArgs(message));
            };

            _webView.CoreWebView2.NavigationStarting += (s, e) =>
            {
                _loadProgressBar.Visibility = Visibility.Visible;
                NavigationStarting?.Invoke(this, e);
            };

            _webView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                _loadProgressBar.Visibility = Visibility.Collapsed;
                NavigationCompleted?.Invoke(this, e);
            };

            // Block ad and tracker requests from the blocklist (the main page itself is never blocked)
            _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            _webView.CoreWebView2.WebResourceRequested += (s, e) =>
            {
                if (e.ResourceContext == CoreWebView2WebResourceContext.Document)
                    return;
                if (ShouldBlockRequest == null)
                    return;
                if (!Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out Uri uri))
                    return;

                if (ShouldBlockRequest(uri))
                {
                    e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(null, 403, "Blocked", "");
                    RequestBlocked?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        private void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs args)
        {
            // Handle pop-ups / new windows if needed – you can raise an event here
        }

        public async Task InjectScriptAsync(string script)
        {
            if (CoreWebView2 != null)
                await CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
        }

        public async Task ExecuteScriptAsync(string script)
        {
            if (CoreWebView2 != null)
                await CoreWebView2.ExecuteScriptAsync(script);
        }

        public void NavigateTo(string url) => CoreWebView2?.Navigate(url);
        public void GoBack() => CoreWebView2?.GoBack();
        public void Reload() => CoreWebView2?.Reload();
    }
}