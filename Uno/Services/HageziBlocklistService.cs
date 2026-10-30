using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Un0.Services
{
    // Downloads the Hagezi "pro" blocklists and answers "is this web address an ad or tracker?"
    // The lists are kept in a copy inside the Windows temp folder, so the next start is instant
    // and blocking is already active before the page loads.
    public class HageziBlocklistService
    {
        private static readonly string[] ListUrls =
        {
            "https://cdn.jsdelivr.net/gh/hagezi/dns-blocklists@latest/adblock/pro.txt",
            "https://cdn.jsdelivr.net/gh/hagezi/dns-blocklists@latest/dnsmasq/pro.txt",
            "https://cdn.jsdelivr.net/gh/hagezi/dns-blocklists@latest/wildcard/pro.txt",
            "https://cdn.jsdelivr.net/gh/hagezi/dns-blocklists@latest/wildcard/pro-onlydomains.txt",
            "https://cdn.jsdelivr.net/gh/hagezi/dns-blocklists@latest/rpz/pro.txt"
        };

        private readonly string _cacheFolder;
        private volatile HashSet<string> _blockedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool IsReady { get; private set; }
        public int DomainCount => _blockedDomains.Count;
        public string CacheFolder => _cacheFolder;

        public event EventHandler Ready;

        public HageziBlocklistService()
        {
            _cacheFolder = Path.Combine(Path.GetTempPath(), "Un0", "Blocklists");
        }

        // Reads the saved copy first (milliseconds), then fetches newer lists in the background.
        // On the very first start there is no saved copy, so the download is awaited once.
        public async Task LoadAsync()
        {
            await LoadFromCacheAsync();

            if (IsReady)
            {
                // Blocking already works - refresh without holding up the browser.
                _ = Task.Run(() => RefreshFromWebAsync());
                return;
            }

            await RefreshFromWebAsync();
        }

        // ----- Step 1: the copy in the temp folder -----
        private async Task LoadFromCacheAsync()
        {
            string[] texts = new string[ListUrls.Length];

            try
            {
                Directory.CreateDirectory(_cacheFolder);
            }
            catch
            {
                // Without a cache folder we simply download straight into memory.
                return;
            }

            bool any = false;
            for (int i = 0; i < ListUrls.Length; i++)
            {
                string cachePath = CachePath(i);
                try
                {
                    if (!File.Exists(cachePath))
                        continue;

                    texts[i] = await File.ReadAllTextAsync(cachePath);
                    any = true;
                }
                catch { }
            }

            if (!any)
                return;

            var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string text in texts)
            {
                if (text != null)
                    ParseInto(text, domains);
            }

            if (domains.Count == 0)
                return;

            _blockedDomains = domains;
            IsReady = true;
            Ready?.Invoke(this, EventArgs.Empty);
        }

        // ----- Step 2: newer lists from the web, saved back into the temp folder -----
        private async Task RefreshFromWebAsync()
        {
            string[] texts = new string[ListUrls.Length];
            bool anyDownloaded = false;

            try
            {
                Directory.CreateDirectory(_cacheFolder);
            }
            catch { }

            try
            {
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
                {
                    for (int i = 0; i < ListUrls.Length; i++)
                    {
                        try
                        {
                            string text = await http.GetStringAsync(ListUrls[i]);
                            texts[i] = text;
                            anyDownloaded = true;

                            try { File.WriteAllText(CachePath(i), text); } catch { }
                        }
                        catch
                        {
                            // Offline or slow: keep using the saved copy for this list.
                            try
                            {
                                string cachePath = CachePath(i);
                                if (File.Exists(cachePath))
                                    texts[i] = File.ReadAllText(cachePath);
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }

            // Nothing came back and nothing was saved - keep whatever is already loaded.
            if (!anyDownloaded)
                return;

            var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string text in texts)
            {
                if (text != null)
                    ParseInto(text, domains);
            }

            if (domains.Count == 0)
                return;

            _blockedDomains = domains;
            IsReady = true;
            Ready?.Invoke(this, EventArgs.Empty);
        }

        private string CachePath(int index) => Path.Combine(_cacheFolder, "list" + index + ".txt");

        // True when the address, or a parent domain of it, is on a blocklist.
        // Example: ads.example.com is blocked if example.com or ads.example.com is listed.
        public bool IsBlocked(Uri uri)
        {
            if (uri == null || !IsReady)
                return false;

            string scheme = uri.Scheme;
            if (scheme != "http" && scheme != "https" && scheme != "ws" && scheme != "wss")
                return false;

            string host = uri.Host.ToLowerInvariant().TrimEnd('.');
            var set = _blockedDomains;

            while (host.Contains('.'))
            {
                if (set.Contains(host))
                    return true;

                int dot = host.IndexOf('.');
                host = host.Substring(dot + 1);
            }

            return false;
        }

        private static void ParseInto(string text, HashSet<string> domains)
        {
            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '!' || line[0] == '#' || line[0] == '[')
                    continue;

                string domain = null;

                if (line.StartsWith("||"))
                {
                    // adblock format: ||example.com^
                    int end = line.IndexOfAny(new[] { '^', '$', '/', '|' }, 2);
                    if (end > 2)
                        domain = line.Substring(2, end - 2);
                }
                else if (line.StartsWith("address=/"))
                {
                    // dnsmasq format: address=/example.com/0.0.0.0
                    string[] parts = line.Split('/');
                    if (parts.Length >= 3)
                        domain = parts[1];
                }
                else
                {
                    // wildcard, plain domain list, rpz ("example.com CNAME .") and hosts ("0.0.0.0 example.com")
                    string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                        continue;

                    domain = parts[0];
                    if ((domain == "0.0.0.0" || domain == "127.0.0.1") && parts.Length > 1)
                        domain = parts[1];
                }

                domain = NormalizeDomain(domain);
                if (domain != null)
                    domains.Add(domain);
            }
        }

        private static string NormalizeDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return null;

            domain = domain.Trim().TrimEnd('.').TrimStart('*', '.').ToLowerInvariant();

            if (!domain.Contains('.'))
                return null;

            foreach (char c in domain)
            {
                bool ok = char.IsLetterOrDigit(c) || c == '-' || c == '.' || c == '_';
                if (!ok)
                    return null;
            }

            return domain;
        }
    }
}
