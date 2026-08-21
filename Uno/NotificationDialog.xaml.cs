using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Un0
{
    public partial class NotificationDialog : Window
    {
        public NotificationDialog()
        {
            InitializeComponent();
            Loaded += NotificationDialog_Loaded;
        }

        private void NotificationDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (Resources["PopupAnimation"] is Storyboard sb)
            {
                sb.Begin(this);
            }
        }

        public void CenterOverWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;

            try
            {
                if (User32API.GetWindowRect(hWnd, out RECT rc))
                {
                    int ownerWidth = rc.Right - rc.Left;
                    int ownerHeight = rc.Bottom - rc.Top;
                    int ownerCenterX = rc.Left + (ownerWidth / 2);
                    int ownerCenterY = rc.Top + (ownerHeight / 2);

                    Left = ownerCenterX - (Width / 2);
                    Top = ownerCenterY - (Height / 2);
                    WindowStartupLocation = WindowStartupLocation.Manual;
                }
            }
            catch { /* fallback to CenterScreen */ }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Just close — no DialogResult needed
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
}