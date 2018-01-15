namespace Termix
{
    public partial class VoiceAssistant
    {
        private void ActionRename(string newName)
        {
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

        private void ActionClose()
        {
            Speak("Closing myself");
            closeMainWindow();
        }

        private void ActionType(string textToType)
        {
            Speak("Typing: " + textToType);
            TypeText(textToType);
        }

        private void ActionSearch(string searchText)
        {
            Speak("Searching for " + searchText);
            Windows.OpenURLInWebBrowser("https://www.google.com/search?q=" + searchText.Replace(' ', '+'));
        }

        private void ActionOpenWeatherForecast()
        {
            Speak("Opening weather forecast");
            Windows.OpenURLInWebBrowser("https://www.google.com/search?q=weather+forecast");
        }

        private void ActionOpenUserDirectory(string dirName)
        {
            Speak("Opening your " + dirName + " directory");
            Windows.OpenDirectoryInExplorer("%userprofile%\\" + dirName);
        }
    }
}