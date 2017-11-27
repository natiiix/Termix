using System;
using System.Diagnostics;

namespace Termix
{
    public static class WindowsTools
    {
        public static void OpenDirectoryInExplorer(string dirPath)
        {
            Process.Start("explorer", Environment.ExpandEnvironmentVariables(dirPath));
        }

        public static void OpenURLInWebBrowser(string url)
        {
            Process.Start(url);
        }

        public static void TypeText(string textToType)
        {
            System.Windows.Forms.SendKeys.SendWait(textToType);
        }
    }
}