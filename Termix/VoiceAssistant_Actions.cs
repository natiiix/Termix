using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Web;
using System.Text.RegularExpressions;

namespace Termix
{
    public partial class VoiceAssistant
    {
        private const double ACTIVATION_SENSITIVITY_STEP = 0.01;
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

        private void ActionIncreaseActivationSensitivity(string[] args)
        {
            double change = ACTIVATION_SENSITIVITY_STEP;

            if (args[0] != string.Empty)
            {
                change = HelperFunctions.GetNumberFromString(args[0]);

                if (double.IsNaN(change))
                {
                    Speak(args[0] + " is not a valid numeric value");
                    return;
                }
            }

            double newSens = Properties.Settings.Default.ActivationSensitivity + change;

            if (newSens > MAXIMUM_ACTIVATION_SENSITIVITY)
            {
                Speak("Setting actication sensitivity to its maximum value");
                Properties.Settings.Default.ActivationSensitivity = MAXIMUM_ACTIVATION_SENSITIVITY;
            }
            else
            {
                Speak("Increasing activation sensitivity to " + newSens.ToString());
                Properties.Settings.Default.ActivationSensitivity = newSens;
            }

            Properties.Settings.Default.Save();
        }

        private void ActionDecreaseActivationSensitivity(string[] args)
        {
            double change = ACTIVATION_SENSITIVITY_STEP;

            if (args[0] != string.Empty)
            {
                change = HelperFunctions.GetNumberFromString(args[0]);

                if (double.IsNaN(change))
                {
                    Speak(args[0] + " is not a valid numeric value");
                    return;
                }
            }

            double newSens = Properties.Settings.Default.ActivationSensitivity - change;

            if (newSens < MINIMUM_ACTIVATION_SENSITIVITY)
            {
                Speak("Setting actication sensitivity to its minimum value");
                Properties.Settings.Default.ActivationSensitivity = MINIMUM_ACTIVATION_SENSITIVITY;
            }
            else
            {
                Speak("Decreasing activation sensitivity to " + newSens.ToString());
                Properties.Settings.Default.ActivationSensitivity = newSens;
            }

            Properties.Settings.Default.Save();
        }

        private void ActionResetSettings(string[] args)
        {
            Speak("Resetting assistant settings to their default values");

            Properties.Settings.Default.Reset();
            invokeDispatcher(() => LoadAssistantName());
        }

        private void ActionType(string[] args)
        {
            Speak("Typing: " + args[0]);
            TypeText(args[0]);
        }

        private void ActionSearch(string[] args)
        {
            Speak("Searching for " + args[0]);
            HelperFunctions.GoogleSearch(args[0]);
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

        private void ActionPressEnter(string[] args)
        {
            Speak("Pressing enter");
            SendKeys.SendWait("{ENTER}");
        }

        private void ActionPressSpace(string[] args)
        {
            Speak("Pressing spacebar");
            SendKeys.SendWait(" ");
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
            string musician = string.IsNullOrEmpty(args[0]) ? args[1] : args[0];

            string response = httpClient.GetStringAsync("https://www.youtube.com/results?search_query=" + HttpUtility.UrlEncode(musician)).Result;
            string decoded = HttpUtility.HtmlDecode(response);

            Regex regex = new Regex("<a href=\"(/watch\\?v=.+?&list=.+?)\"[^<>]*aria-label=\"Mix YouTube\">");
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

        private void ActionReadTime(string[] args)
        {
            Speak("It is currently " + DateTime.Now.ToShortTimeString());
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
    }
}
