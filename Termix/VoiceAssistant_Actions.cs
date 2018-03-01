using System.Windows.Forms;

namespace Termix
{
    public partial class VoiceAssistant
    {
        private void ActionRename(string[] args)
        {
            if (args.Length != 1)
            {
                return;
            }

            string newName = args[0];

            // User is trying to set the assistant's name to an empty string
            if (newName == string.Empty)
            {
                Speak("The assistant must have a name!");
            }
            else
            {
                Speak("Changing my name to " + newName);
                invokeDispatcher(() => SetAssistantName(newName));
            }
        }

        private void ActionClose(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Closing myself");
            closeMainWindow();
        }

        private void ActionType(string[] args)
        {
            if (args.Length != 1)
            {
                return;
            }

            string textToType = args[0];

            Speak("Typing: " + textToType);
            TypeText(textToType);
        }

        private void ActionSearch(string[] args)
        {
            if (args.Length != 1)
            {
                return;
            }

            string searchText = args[0];

            Speak("Searching for " + searchText);
            Windows.OpenURLInWebBrowser("https://www.google.com/search?q=" + searchText.Replace(' ', '+'));
        }

        private void ActionOpenWeatherForecast(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Opening weather forecast");
            Windows.OpenURLInWebBrowser("https://www.google.com/search?q=weather+forecast");
        }

        private void ActionOpenUserDirectory(string[] args)
        {
            if (args.Length != 1)
            {
                return;
            }

            string dirName = args[0];

            Speak("Opening your " + dirName + " directory");
            Windows.OpenDirectoryInExplorer("%userprofile%\\" + dirName);
        }

        private void ActionPressEnter(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Pressing enter");
            SendKeys.SendWait("{ENTER}");
        }

        private void ActionPressSpace(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Pressing spacebar");
            SendKeys.SendWait(" ");
        }
    }
}
