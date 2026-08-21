using System.Threading.Tasks;

namespace Un0.Services
{
    public class VolumeBoostService
    {
        private WebViewManager _webViewManager;
        private StatusBarService _statusBar;
        private bool _isBoosted = false;

        public VolumeBoostService(WebViewManager webViewManager, StatusBarService statusBar)
        {
            _webViewManager = webViewManager;
            _statusBar = statusBar;
            _webViewManager.WebMessageReceived += OnWebMessageReceived;
        }

        public string GetVolumeBoostScript()
        {
            return @"
                (function() {
                    let isBoosted = false;
                    let originalVolume = 1.0;
                    function boostVolume() {
                        // ... your volume boost logic ...
                    }
                    window.addEventListener('message', function(event) {
                        if (event.data === 'toggle_volume_boost') boostVolume();
                    });
                })();
            ";
        }

        private void OnWebMessageReceived(object sender, WebMessageEventArgs e)
        {
            if (e.Message.Contains("volume_boosted_on"))
                _statusBar.SetStatus("🔊 Volume Boost ON (400%)", "#00FF88");
            else if (e.Message.Contains("volume_boosted_off"))
                _statusBar.SetStatus("🔊 Volume Boost OFF", "#666666");
        }

        public async Task ToggleBoost()
        {
            _isBoosted = !_isBoosted;
            await _webViewManager.ExecuteScriptAsync("window.postMessage('toggle_volume_boost');");
        }

        public bool IsBoosted => _isBoosted;
    }
}