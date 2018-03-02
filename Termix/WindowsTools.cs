using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Termix
{
    public static class Windows
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
    }
}
