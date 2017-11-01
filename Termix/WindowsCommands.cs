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
    }
}