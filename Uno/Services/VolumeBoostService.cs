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
                        const videos = document.getElementsByTagName('video');
                        let boosted = false;
                        
                        if (!isBoosted) {
                            // Store original volume from the first video that has volume > 0.1
                            for (let video of videos) {
                                if (video.volume !== undefined && video.volume > 0.1) {
                                    originalVolume = video.volume;
                                    break;
                                }
                            }
                            // Boost all videos to 4x (capped at 4.0)
                            for (let video of videos) {
                                if (video.volume !== undefined) {
                                    video.volume = Math.min(4.0, originalVolume * 4);
                                    boosted = true;
                                }
                            }
                            isBoosted = true;
                            window.chrome.webview.postMessage('volume_boosted_on');
                        } else {
                            // Restore original volume
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