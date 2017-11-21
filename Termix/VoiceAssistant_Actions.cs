namespace Termix
{
    public partial class VoiceAssistant
    {
        private void ActionRename(string newName)
        {
            Speak("Renaming myself to " + newName);
            invokeDispatcher(() => SetAssistantName(newName));
        }

        private void ActionClose()
        {
            Speak("Closing myself");
            closeMainWindow();
        }

        private void ActionType(string textToType)
        {
            Speak("Typing " + textToType);
            WindowsCommands.TypeText(textToType);
        }

        private void ActionSearch(string searchText)
        {
            Speak("Searching for " + searchText);
            WindowsCommands.OpenURLInWebBrowser("https://www.google.com/search?q=" + searchText.Replace(' ', '+'));
        }

        private void ActionOpenWeatherForecast()
        {
            Speak("Opening weather forecast");
            WindowsCommands.OpenURLInWebBrowser("https://www.google.com/search?q=weather+forecast");
        }

        private void ActionOpenUserDirectory(string dirName)
        {
            Speak("Opening your " + dirName + " directory");
            WindowsCommands.OpenDirectoryInExplorer("%userprofile%\\" + dirName);
        }
    }
}