using System.Windows.Forms;

namespace Termix
{
    public partial class VoiceAssistant
    {
        private const double ACTIVATION_SENSITIVITY_STEP = 0.01;
        private const double MINIMUM_ACTIVATION_SENSITIVITY = 0.05;
        private const double MAXIMUM_ACTIVATION_SENSITIVITY = 0.4;

        private void ActionAssistantRename(string[] args)
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

                Properties.Settings.Default.Name = newName;
                Properties.Settings.Default.Save();

                invokeDispatcher(() => LoadAssistantName());
            }
        }

        private void ActionAssistantShutDown(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Shutting down the assistant");
            closeMainWindow();
        }

        private void ActionIncreaseActivationSensitivity(string[] args)
        {
            if (args.Length != 1)
            {
                return;
            }

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
            if (args.Length != 1)
            {
                return;
            }

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
            if (args.Length != 0)
            {
                return;
            }

            Speak("Resetting assistant settings to their default values");

            Properties.Settings.Default.Reset();
            invokeDispatcher(() => LoadAssistantName());
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
            HelperFunctions.GoogleSearch(searchText);
        }

        private void ActionOpenWeatherForecast(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Opening weather forecast");
            HelperFunctions.GoogleSearch("weather forecast");
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

        private void ActionSolveMathProblem(string[] args)
        {
            if (args.Length != 3)
            {
                return;
            }

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
            if (args.Length != 1)
            {
                return;
            }

            Speak("Searching for a solution to " + args[0]);
            HelperFunctions.GoogleSearch(args[0]);
        }

        private void ActionScrollDown(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Scrolling down");
            SendKeysWait("{PGDN}");
        }

        private void ActionScrollUp(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Scrolling up");
            SendKeysWait("{PGUP}");
        }

        private void ActionCloseWindow(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Closing the active window");
            SendKeysWait("%{F4}");
        }

        private void ActionBrowserNewTab(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Opening a new tab");
            SendKeysWait("^{t}");
        }

        private void ActionBrowserCloseTab(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Closing the active tab");
            SendKeysWait("^{w}");
        }

        private void ActionBrowserReopenTab(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Reopening the last closed tab");
            SendKeysWait("^+{t}");
        }

        private void ActionBrowserNextTab(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Switching to the next tab");
            SendKeysWait("^{TAB}");
        }

        private void ActionBrowserPreviousTab(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            Speak("Switching to the previous tab");
            SendKeysWait("^+{TAB}");
        }
    }
}
