using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace Un0.Services
{
    public class ProtectionStatusService
    {
        private TextBlock _protectionLabel;
        private TextBlock _protectionIcon;

        public ProtectionStatusService(TextBlock protectionIcon, TextBlock protectionLabel)
        {
            _protectionIcon = protectionIcon;
            _protectionLabel = protectionLabel;
        }

        public void UpdateProtectionStatus(int adBlockCounter)
        {
            if (_protectionLabel == null || _protectionIcon == null) return;

            string color;
            if (adBlockCounter > 20)
                color = "#FF6B6B";
            else if (adBlockCounter > 10)
                color = "#FFD93D";
            else
                color = "#00FF88";

            var converter = new BrushConverter();
            _protectionLabel.Foreground = (Brush)converter.ConvertFromString(color);
            _protectionIcon.Foreground = (Brush)converter.ConvertFromString(color);
            _protectionLabel.Text = $"Protected: {adBlockCounter}";
        }
    }
}