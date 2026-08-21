using System;

namespace Un0.Services
{
    public class AdBlockerService
    {
        private WebViewManager _webViewManager;
        private ProtectionStatusService _protectionStatus;
        private int _adBlockCounter = 0;

        public event EventHandler<int> AdBlocked;

        public AdBlockerService(WebViewManager webViewManager, ProtectionStatusService protectionStatus)
        {
            _webViewManager = webViewManager;
            _protectionStatus = protectionStatus;
            // Subscribe to WebView's WebMessageReceived to catch ad_blocked messages
            _webViewManager.WebMessageReceived += OnWebMessageReceived;
        }

        private void OnWebMessageReceived(object sender, WebMessageEventArgs e)
        {
            if (e.Message.Contains("ad_blocked_"))
            {
                _adBlockCounter++;
                _protectionStatus.UpdateProtectionStatus(_adBlockCounter);
                AdBlocked?.Invoke(this, _adBlockCounter);
            }
        }

        public string GetAdBlockScript()
        {
            return @"
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
        }

        public void ResetCounter()
        {
            _adBlockCounter = 0;
            _protectionStatus.UpdateProtectionStatus(0);
        }

        public int GetCurrentCount() => _adBlockCounter;
    }
}