using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Un0.Services
{
    // Checks the internet connection every few seconds and shows the ping time.
    public class InternetPingService
    {
        private readonly TextBlock _label;
        private readonly DispatcherTimer _timer;
        private readonly string[] _hosts = { "1.1.1.1", "8.8.8.8" };
        private bool _busy;

        public InternetPingService(TextBlock label)
        {
            _label = label;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _timer.Tick += async (s, e) => await PingOnceAsync();
        }

        public void Start()
        {
            _ = PingOnceAsync();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private async Task PingOnceAsync()
        {
            if (_busy || _label == null) return;
            _busy = true;
            try
            {
                long? ms = await PingAnyHostAsync();

                if (ms == null)
                    Show("Ping: offline", "#FF4444");
                else if (ms < 100)
                    Show("Ping: " + ms + " ms", "#00FF88");
                else if (ms < 250)
                    Show("Ping: " + ms + " ms", "#FFCC00");
                else
                    Show("Ping: " + ms + " ms (unstable)", "#FF8800");
            }
            finally
            {
                _busy = false;
            }
        }

        // Tries each host in turn; returns the round-trip time, or null if none answered.
        private async Task<long?> PingAnyHostAsync()
        {
            foreach (var host in _hosts)
            {
                try
                {
                    using (var ping = new Ping())
                    {
                        var reply = await ping.SendPingAsync(host, 2000);
                        if (reply.Status == IPStatus.Success)
                            return reply.RoundtripTime;
                    }
                }
                catch
                {
                    // Try the next host
                }
            }
            return null;
        }

        private void Show(string text, string colorHex)
        {
            _label.Text = text;
            try
            {
                _label.Foreground = (Brush)new BrushConverter().ConvertFromString(colorHex);
            }
            catch { }
        }
    }
}
