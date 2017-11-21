using System;
using System.Diagnostics;

namespace Termix
{
    public static class WindowsCommands
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