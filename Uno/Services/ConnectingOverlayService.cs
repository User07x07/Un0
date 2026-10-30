using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Un0.Services
{
    // Shows a "Un0 is stabilizing, please wait..." panel with animated dots while the first page loads
    public class ConnectingOverlayService
    {
        private Grid _overlay;
        private TextBlock _dots;
        private DispatcherTimer _timer;
        private int _step;

        public void Show(Panel container)
        {
            if (container == null || _overlay != null)
                return;

            var title = new TextBlock
            {
                Text = "Un0 is stabilizing, please wait",
                Foreground = (Brush)new BrushConverter().ConvertFromString("#00FF88"),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _dots = new TextBlock
            {
                Text = "...",
                Foreground = (Brush)new BrushConverter().ConvertFromString("#00FF88"),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(title);
            stack.Children.Add(_dots);

            _overlay = new Grid
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#111111"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _overlay.Children.Add(stack);
            Panel.SetZIndex(_overlay, 1000);

            container.Children.Add(_overlay);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _timer.Tick += (s, e) =>
            {
                _step = (_step + 1) % 4;
                _dots.Text = new string('.', _step);
            };
            _timer.Start();
        }

        public void Hide()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }

            if (_overlay != null)
            {
                if (_overlay.Parent is Panel parent)
                    parent.Children.Remove(_overlay);
                _overlay = null;
                _dots = null;
            }
        }
    }
}
