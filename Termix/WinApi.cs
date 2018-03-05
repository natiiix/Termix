using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Termix
{
    public static class WinApi
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        public static Process GetForegroundProcess()
        {
            IntPtr hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out uint pid);
            Process p = Process.GetProcessById((int)pid);
            return p;
        }

        public static void OpenDirectoryInExplorer(string dirPath)
        {
            Process.Start("explorer", Environment.ExpandEnvironmentVariables(dirPath));
        }

        public static void OpenURLInWebBrowser(string url)
        {
            Process.Start(url);
        }

        public static class Keyboard
        {
            [DllImport("user32.dll")]
#pragma warning disable IDE1006 // Naming Styles
            private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

#pragma warning restore IDE1006 // Naming Styles

            private const uint KEYEVENTF_KEYUP = 0x0002;

            private static byte KeyToByte(Key key) => (byte)KeyInterop.VirtualKeyFromKey(key);

            public static void Press(Key key)
            {
                Down(key);
                Up(key);
            }

            public static void Down(Key key) => keybd_event(KeyToByte(key), 0, 0, 0);

            public static void Up(Key key) => keybd_event(KeyToByte(key), 0, KEYEVENTF_KEYUP, 0);
        }

        public static class Mouse
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
#pragma warning disable IDE1006 // Naming Styles
            private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

#pragma warning restore IDE1006 // Naming Styles

            private const int MOUSEEVENTF_LEFTDOWN = 0x02;
            private const int MOUSEEVENTF_LEFTUP = 0x04;

            private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
            private const int MOUSEEVENTF_RIGHTUP = 0x10;

            private const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
            private const int MOUSEEVENTF_MIDDLEUP = 0x40;

            private static void MouseClick(uint flags)
            {
                uint X = (uint)System.Windows.Forms.Cursor.Position.X;
                uint Y = (uint)System.Windows.Forms.Cursor.Position.X;
                mouse_event(flags, X, Y, 0, 0);
            }

            public static void LeftClick() => MouseClick(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP);

            public static void RightClick() => MouseClick(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP);

            public static void MiddleClick() => MouseClick(MOUSEEVENTF_MIDDLEDOWN | MOUSEEVENTF_MIDDLEUP);
        }
    }
}
