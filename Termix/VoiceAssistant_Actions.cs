using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Web;
using System.Text.RegularExpressions;
using System.Drawing;

namespace Termix
{
    public partial class VoiceAssistant
    {
        private const double ACTIVATION_SENSITIVITY_STEP = 0.05;
        private const double MINIMUM_ACTIVATION_SENSITIVITY = 0.05;
        private const double MAXIMUM_ACTIVATION_SENSITIVITY = 0.4;

        private void ActionAssistantRename(string[] args)
        {
            // User is trying to set the assistant's name to an empty string
            if (string.IsNullOrEmpty(args[0]))
            {
                Speak("The assistant must have a name!");
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
            Windows.OpenDirectoryInExplorer("%userprofile%\\" + args[0]);
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
                Windows.OpenURLInWebBrowser("https://www.youtube.com" + match.Groups[1].Value);
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
                Windows.OpenURLInWebBrowser("https://www.youtube.com" + match.Groups[1].Value);
            }
            else
            {
                Speak($"Unable to find a YouTube video called {args[0]}");
            }
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

            switch (args[1])
            {
                case "left":
                    Cursor.Position = new Point(Math.Max(Cursor.Position.X - distance, 0), Cursor.Position.Y);
                    break;

                case "right":
                    Cursor.Position = new Point(Math.Min(Cursor.Position.X + distance, Screen.PrimaryScreen.Bounds.Width - 1), Cursor.Position.Y);
                    break;

                case "up":
                    Cursor.Position = new Point(Cursor.Position.X, Math.Max(Cursor.Position.Y - distance, 0));
                    break;

                case "down":
                    Cursor.Position = new Point(Cursor.Position.X, Math.Min(Cursor.Position.Y + distance, Screen.PrimaryScreen.Bounds.Height - 1));
                    break;

                default:
                    break;
            }
        }

        private void ActionCloseWindow(string[] args)
        {
            Speak("Closing the active window");
            SendKeysWait("%{F4}");
        }

        private void ActionOpenCalc(string[] args)
        {
            Speak("Opening the calculator");
            Process.Start("calc");
        }

        private void ActionOpenPaint(string[] args)
        {
            Speak("Opening the MS paint");
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
            Windows.OpenURLInWebBrowser($"http://{args[0]}.com");
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
