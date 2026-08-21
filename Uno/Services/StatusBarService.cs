using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace Un0.Services
{
    public class StatusBarService
    {
        private TextBlock _statusLabel;

        public StatusBarService(TextBlock statusLabel)
        {
            _statusLabel = statusLabel;
        }

        public void SetStatus(string text, string colorHex = "#666666")
        {
            if (_statusLabel == null) return;
            _statusLabel.Text = text;
            try
            {
                var converter = new BrushConverter();
                _statusLabel.Foreground = (Brush)converter.ConvertFromString(colorHex);
            }
            catch { }
        }

        public async void SetTemporaryStatus(string text, string colorHex, int durationMs)
        {
            SetStatus(text, colorHex);
            await Task.Delay(durationMs);
            SetStatus("Ready", "#666666");
        }
    }
}