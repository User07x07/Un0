using System;
using System.Runtime.InteropServices;

namespace Un0
{
    public class User32API
    {
        [DllImport("User32.dll", SetLastError = true)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        public const int SW_RESTORE = 9;
        public const int SW_SHOW = 5;
    }
}