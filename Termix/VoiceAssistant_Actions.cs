using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;

namespace Termix
{
    public partial class VoiceAssistant
    {
        private const double ACTIVATION_SENSITIVITY_STEP = 0.05;
        private const double MINIMUM_ACTIVATION_SENSITIVITY = 0.05;
        private const double MAXIMUM_ACTIVATION_SENSITIVITY = 0.4;

        private void ActionStopListening(string[] args)
        {
            Speak("Okay");
            GoogleSpeechRecognizer.StopListening = true;
        }

        private void ActionAssistantRename(string[] args)
        {
            // User is trying to set the assistant's name to an empty string
            if (string.IsNullOrEmpty(args[0]))
            {
                Speak("The assistant must have a name");
            }
            else
            {
                Speak("Changing my name to " + args[0]);

                Properties.Settings.Default.Name = args[0];
                Properties.Settings.Default.Save();

                invokeDispatcher(() => LoadAssistantName());
            }
        }

        private void ActionAssistantShutDown(string[] args)
        {
            Speak("Shutting down the assistant");
            GoogleSpeechRecognizer.StopListening = true;
            closeMainWindow();
        }

        private void ActionChangeActivationSensitivity(string[] args)
        {
            double change = ACTIVATION_SENSITIVITY_STEP;

            if (args[1] != string.Empty)
            {
                change = HelperFunctions.GetDoubleFromString(args[1]);

                if (double.IsNaN(change))
                {
                    Speak(args[1] + " is not a valid numeric value");
                    return;
                }
            }

            if (args[0] == "decrease")
            {
                change = -change;
            }

            double newSens = Properties.Settings.Default.ActivationSensitivity + change;

            if (newSens > MAXIMUM_ACTIVATION_SENSITIVITY)
            {
                Speak($"Setting the actication sensitivity to its maximum value {MAXIMUM_ACTIVATION_SENSITIVITY}");
                Properties.Settings.Default.ActivationSensitivity = MAXIMUM_ACTIVATION_SENSITIVITY;
            }
            else if (newSens < MINIMUM_ACTIVATION_SENSITIVITY)
            {
                Speak($"Setting the actication sensitivity to its minimum value {MINIMUM_ACTIVATION_SENSITIVITY}");
                Properties.Settings.Default.ActivationSensitivity = MINIMUM_ACTIVATION_SENSITIVITY;
            }
            else
            {
                Speak($"Changing the activation sensitivity to {newSens}");
                Properties.Settings.Default.ActivationSensitivity = newSens;
            }

            Properties.Settings.Default.Save();
        }

        private void ActionSetVoiceFeedback(string[] args)
        {
            Properties.Settings.Default.VoiceFeedback = args[0] == "enable";
            Properties.Settings.Default.Save();

            Speak($"Voice feedback has been {args[0]}d");
        }

        private void ActionResetSettings(string[] args)
        {
            Properties.Settings.Default.Reset();
            invokeDispatcher(() => LoadAssistantName());

            Speak("All assistant settings have been reset to their default values");
        }

        private void ActionEnterData(string[] args)
        {
            Speak("Entering " + args[0]);
            TypeText(data.Aliases.First(x => x.Regex.IsMatch(args[0])).Value);
        }

        private void ActionType(string[] args)
        {
            Speak("Typing: " + args[0]);
            TypeText(args[0]);
        }

        private void ActionSearch(string[] args)
        {
            string site = string.IsNullOrEmpty(args[1]) ? "Google" : args[1];

            Speak($"Searching for {args[0]} on {site}");

            switch (site)
            {
                case "Google":
                    HelperFunctions.GoogleSearch(args[0]);
                    break;

                case "YouTube":
                    HelperFunctions.YouTubeSearch(args[0]);
                    break;

                case "Wikipedia":
                    HelperFunctions.WikipediaSearch(args[0]);
                    break;

                default:
                    break;
            }
        }

        private void ActionOpenWeatherForecast(string[] args)
        {
            Speak("Opening weather forecast");
            HelperFunctions.GoogleSearch("weather forecast");
        }

        private void ActionOpenUserDirectory(string[] args)
        {
            Speak("Opening your " + args[0] + " directory");
            WinApi.OpenDirectoryInExplorer("%userprofile%\\" + args[0]);
        }

        private void ActionSolveMathProblem(string[] args)
        {
            if (double.TryParse(args[0], out double leftOperand) && double.TryParse(args[2], out double rightOperand))
            {
                switch (args[1])
                {
                    case "+":
                        Speak($"{leftOperand} plus {rightOperand} is equal to {leftOperand + rightOperand}");
                        break;

                    case "-":
                        Speak($"{leftOperand} minus {rightOperand} is equal to {leftOperand - rightOperand}");
                        break;

                    case "*":
                        Speak($"{leftOperand} multiplied by {rightOperand} is equal to {leftOperand * rightOperand}");
                        break;

                    case "/":
                        if (rightOperand == 0d)
                        {
                            Speak("It is impossible to divide by zero");
                        }
                        else
                        {
                            Speak($"{leftOperand} divided by {rightOperand} is equal to {leftOperand / rightOperand}");
                        }
                        break;

                    default:
                        break;
                }
            }
            else
            {
                ActionGoogleMathProblem(new string[] { string.Join(" ", args[0], args[1], args[2]) });
            }
        }

        private void ActionGoogleMathProblem(string[] args)
        {
            Speak("Searching for a solution to " + args[0]);
            HelperFunctions.GoogleSearch(args[0]);
        }

        private void ActionPlayYouTubeMix(string[] args)
        {
            string musician = HelperFunctions.GetNonEmptyString(args);

            string response = httpClient.GetStringAsync(HelperFunctions.GetYouTubeSearchURL(musician)).Result;
            string decoded = HttpUtility.HtmlDecode(response);

            Regex regex = new Regex("<a href=\"(/watch\\?v=[a-zA-Z0-9_-]{11}&list=[a-zA-Z0-9_-]+?)\"[^<>]*?aria-label=\"Mix YouTube\">");
            Match match = regex.Match(decoded);

            if (match.Success && match.Groups.Count == 2)
            {
                Speak($"Playing {musician} mix on YouTube");
                WinApi.OpenURLInWebBrowser("https://www.youtube.com" + match.Groups[1].Value);
            }
            else
            {
                Speak($"Unable to find {musician} mix on YouTube");
            }
        }

        private void ActionPlayYouTubeVideo(string[] args)
        {
            string response = httpClient.GetStringAsync(HelperFunctions.GetYouTubeSearchURL(args[0])).Result;
            string decoded = HttpUtility.HtmlDecode(response);

            Regex regex = new Regex("<a[^<>]*?href=\"(/watch\\?v=[a-zA-Z0-9_-]{11})\"[^<>]*?title=\"([^\"]*?)\"[^<>]*?>");
            Match match = regex.Match(decoded);

            if (match.Success && match.Groups.Count == 3)
            {
                Speak($"Playing a YouTube video called {match.Groups[2].Value}");
                WinApi.OpenURLInWebBrowser("https://www.youtube.com" + match.Groups[1].Value);
            }
            else
            {
                Speak($"Unable to find a YouTube video called {args[0]}");
            }
        }

        private void ActionOpenFacebookChat(string[] args)
        {
            Speak("Opening your Facebook chat with " + args[0]);
            WinApi.OpenURLInWebBrowser("https://www.facebook.com/messages/t/" + data.FacebookContacts.First(x => x.Regex.IsMatch(args[0])).Value);
        }

        private void ActionReadTime(string[] args)
        {
            Speak("It is currently " + DateTime.Now.ToShortTimeString());
        }

        private void ActionReadJoke(string[] args)
        {
            string[] jokes = Properties.Resources.JokeDataSet.Split("\r\n");
            Speak(jokes[new Random().Next(jokes.Length)]);
        }

        private void ActionScreenshot(string[] args)
        {
            Speak("Taking a screenshot");

            Rectangle bounds = Screen.PrimaryScreen.Bounds;

            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                invokeDispatcher(() => Clipboard.SetImage(bitmap));
            }
        }

        private void ActionPressKey(string[] args)
        {
            string key = string.Empty;

            Regex regex = new Regex("^(?:(.)|(left|right|up|down)(?: arrow)?|(F(?:[1-9]|1[0-6])|enter|backspace|delete|insert|tab|end|home))$", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match match = regex.Match(args[0]);

            if (match.Success && match.Groups.Count == 4)
            {
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    key = match.Groups[1].Value;
                }
                else
                {
                    key = "{" + HelperFunctions.GetNonEmptyString(match.Groups[2].Value, match.Groups[3].Value).ToUpper() + "}";
                }
            }
            else
            {
                switch (args[0].ToLower())
                {
                    case "space":
                    case "space bar":
                        key = " ";
                        break;

                    case "page up":
                        key = "{PGUP}";
                        break;

                    case "page down":
                        key = "{PGDN}";
                        break;

                    default:
                        break;
                }
            }

            if (!string.IsNullOrEmpty(key))
            {
                if (string.IsNullOrEmpty(args[1]))
                {
                    Speak($"Pressing the {args[0]} key");
                    SendKeys.SendWait(key);
                }
                else
                {
                    int times = HelperFunctions.GetIntFromString(args[1]);

                    Speak($"Pressing the {args[0]} key {times} times");

                    for (int i = 0; i < times; i++)
                    {
                        SendKeys.SendWait(key);
                    }
                }
            }
            else
            {
                Speak(args[0] + " is not a valid name of a key");
            }
        }

        private void ActionSelectAll(string[] args)
        {
            Speak("Selecting all");
            SendKeysWait("^{a}");
        }

        private void ActionClipboard(string[] args)
        {
            switch (args[0])
            {
                case "copy":
                    Speak("Copying");
                    SendKeysWait("^{c}");
                    break;

                case "cut":
                    Speak("Cutting");
                    SendKeysWait("^{x}");
                    break;

                case "paste":
                    Speak("Pasting");
                    SendKeysWait("^{v}");
                    break;

                default:
                    break;
            }
        }

        private void ActionDeleteWord(string[] args)
        {
            Speak("Deleting the last word");

            int times = string.IsNullOrEmpty(args[0]) ? 1 : HelperFunctions.GetIntFromString(args[0]);

            for (int i = 0; i < times; i++)
            {
                SendKeysWait("^{backspace}");
            }
        }

        private void ActionSendMessage(string[] args)
        {
            Speak("Sending the message");
            SendKeysWait("{ENTER}");
        }

        private void ActionScrollDown(string[] args)
        {
            Speak("Scrolling down");
            SendKeysWait("{PGDN}");
        }

        private void ActionScrollUp(string[] args)
        {
            Speak("Scrolling up");
            SendKeysWait("{PGUP}");
        }

        private void ActionMoveCursor(string[] args)
        {
            int distance = HelperFunctions.GetIntFromString(args[0]);

            string messageBase = $"Moving the mouse cursor {distance} pixels";

            switch (args[1])
            {
                case "left":
                    Speak(messageBase + " to the left");
                    Cursor.Position = new Point(Math.Max(Cursor.Position.X - distance, 0), Cursor.Position.Y);
                    break;

                case "right":
                    Speak(messageBase + " to the right");
                    Cursor.Position = new Point(Math.Min(Cursor.Position.X + distance, Screen.PrimaryScreen.Bounds.Width - 1), Cursor.Position.Y);
                    break;

                case "up":
                    Speak(messageBase + " up");
                    Cursor.Position = new Point(Cursor.Position.X, Math.Max(Cursor.Position.Y - distance, 0));
                    break;

                case "down":
                    Speak(messageBase + " down");
                    Cursor.Position = new Point(Cursor.Position.X, Math.Min(Cursor.Position.Y + distance, Screen.PrimaryScreen.Bounds.Height - 1));
                    break;

                default:
                    break;
            }
        }

        private void ActionMouseClick(string[] args)
        {
            Action actionClick = null;

            switch (args[0])
            {
                case "left":
                    actionClick = WinApi.Mouse.LeftClick;
                    break;

                case "right":
                    actionClick = WinApi.Mouse.RightClick;
                    break;

                case "middle":
                    actionClick = WinApi.Mouse.MiddleClick;
                    break;

                default:
                    break;
            }

            if (string.IsNullOrEmpty(args[1]))
            {
                Speak($"Clicking with the {args[0]} mouse button");
                actionClick();
            }
            else
            {
                int times = HelperFunctions.GetIntFromString(args[1]);

                Speak($"Clicking with the {args[0]} mouse button {times} times");

                for (int i = 0; i < times; i++)
                {
                    actionClick();
                }
            }
        }

        private void ActionCloseWindow(string[] args)
        {
            Speak("Closing the active window");
            SendKeysWait("%{F4}");
        }

        private void ActionMuteSound(string[] args)
        {
            switch (args[0])
            {
                case "unmute":
                case "enable":
                    WinApi.Volume.PlaybackMuted = false;
                    Speak("Sound has been unmuted");
                    break;

                case "mute":
                case "disable":
                    WinApi.Volume.PlaybackMuted = true;
                    Speak("Sound has been muted");
                    break;

                default:
                    break;
            }
        }

        private void ActionSetVolume(string[] args)
        {
            int volumePercent = Math.Min(HelperFunctions.GetIntFromString(args[0]), 100);

            Speak($"Changing the sound volume to {volumePercent} percent");
            WinApi.Volume.PlaybackVolume = volumePercent;
        }

        private void ActionChangeVolume(string[] args)
        {
            int volumePercent = string.IsNullOrEmpty(args[1]) ? 2 : HelperFunctions.GetIntFromString(args[1]);
            int oldVolume = WinApi.Volume.PlaybackVolume;

            switch (args[0])
            {
                case "increase":
                    {
                        int newVolume = Math.Min(oldVolume + volumePercent, 100);

                        Speak($"Increasing the sound volume by {newVolume - oldVolume} percent");
                        WinApi.Volume.PlaybackVolume = newVolume;
                    }
                    break;

                case "decrease":
                    {
                        int newVolume = Math.Max(oldVolume - volumePercent, 0);

                        Speak($"Decreasing the sound volume by {oldVolume - newVolume} percent");
                        WinApi.Volume.PlaybackVolume = newVolume;
                    }
                    break;

                default:
                    break;
            }
        }

        private void ActionOpenNotepad(string[] args)
        {
            Speak("Opening the notepad");
            Process.Start("notepad");
        }

        private void ActionOpenCalc(string[] args)
        {
            Speak("Opening the calculator");
            Process.Start("calc");
        }

        private void ActionOpenPaint(string[] args)
        {
            Speak("Opening Microsoft Paint");
            Process.Start("mspaint");
        }

        private void ActionOpenWebBrowser(string[] args)
        {
            Speak("Opening the web browser");
            Process.Start("chrome");
        }

        private void ActionOpenWebpage(string[] args)
        {
            Speak("Opening " + args[0]);
            WinApi.OpenURLInWebBrowser($"http://{args[0]}.com");
        }

        private void ActionBrowserNewTab(string[] args)
        {
            Speak("Opening a new tab");
            SendKeysWait("^{t}");
        }

        private void ActionBrowserCloseTab(string[] args)
        {
            Speak("Closing the active tab");
            SendKeysWait("^{w}");
        }

        private void ActionBrowserReopenTab(string[] args)
        {
            Speak("Reopening the last closed tab");
            SendKeysWait("^+{t}");
        }

        private void ActionBrowserNextTab(string[] args)
        {
            Speak("Switching to the next tab");
            SendKeysWait("^{TAB}");
        }

        private void ActionBrowserPreviousTab(string[] args)
        {
            Speak("Switching to the previous tab");
            SendKeysWait("^+{TAB}");
        }

        private void ActionBrowserBack(string[] args)
        {
            Speak("Going to the previous page");
            SendKeysWait("%{LEFT}");
        }

        private void ActionBrowserForward(string[] args)
        {
            Speak("Going to the next page");
            SendKeysWait("%{RIGHT}");
        }
    }
}
