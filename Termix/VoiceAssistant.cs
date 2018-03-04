using System;
using System.Diagnostics;
using System.Drawing;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Windows.Forms;
using System.Net.Http;

namespace Termix
{
    public partial class VoiceAssistant
    {
        private SpeechRecognitionEngine offlineRecognizer;
        private SpeechSynthesizer synthesizer;
        private VoiceCommandList cmdList;
        private HttpClient httpClient;

        // Invoke dispatcher
        public delegate void InvokeDispatcherCallback(Action action);

        private InvokeDispatcherCallback invokeDispatcher;

        // Close main window
        public delegate void CloseMainWindowCallback();

        private CloseMainWindowCallback closeMainWindow;

        // Set recognition label text
        public delegate void SetRecognitionLabelTextCallback(string strText);

        private SetRecognitionLabelTextCallback setRecognitionLabelText;

        // Set name label text
        public delegate void SetNameLabelTextCallback(string strName);

        private SetNameLabelTextCallback setNameLabelText;

        // Update listening UI
        public delegate void UpdateListeningUICallback(bool listeningInProgress);

        private UpdateListeningUICallback updateListeningUI;

        // Append log
        public delegate void AppendLogCallback(string text);

        private AppendLogCallback appendLog;

        // Constructor
        public VoiceAssistant(
            InvokeDispatcherCallback invokeDispatcherCallback,
            CloseMainWindowCallback closeMainWindowCallback,
            SetRecognitionLabelTextCallback setRecognitionLabelTextCallback,
            SetNameLabelTextCallback setNameLabelTextCallback,
            UpdateListeningUICallback updateListeningUICallback,
            AppendLogCallback appendLogCallback)
        {
            invokeDispatcher = invokeDispatcherCallback;
            closeMainWindow = closeMainWindowCallback;
            setRecognitionLabelText = setRecognitionLabelTextCallback;
            setNameLabelText = setNameLabelTextCallback;
            updateListeningUI = updateListeningUICallback;
            appendLog = appendLogCallback;

            offlineRecognizer = new SpeechRecognitionEngine();
            offlineRecognizer.SpeechRecognized += OfflineRecognizer_SpeechRecognized;
            offlineRecognizer.SetInputToDefaultAudioDevice();
            LoadAssistantName();
            ActivateOfflineRecognizer();

            synthesizer = new SpeechSynthesizer();

            httpClient = new HttpClient();

            cmdList = new VoiceCommandList(x => Speak("I do not understand: " + x));

            // Assistant
            RegisterCommand("do nothing|don't do anything|stop listening", _ => Speak("ok"));
            RegisterCommand("(?:close (?:yourself|the assistant)|shut (?:(?:yourself|the assistant) )?down)", ActionAssistantShutDown);
            RegisterCommand("(?:change (?:your )?(?:name|activation(?: command)?)|rename(?: yourself)?) to (.+)", ActionAssistantRename);
            RegisterCommand(@"increase (?:the )?activation sensitivity(?: by (\d+(?:.\d+|%)?))?", ActionIncreaseActivationSensitivity);
            RegisterCommand(@"decrease (?:the )?activation sensitivity(?: by (\d+(?:.\d+|%)?))?", ActionDecreaseActivationSensitivity);
            RegisterCommand("reset assistant settings", ActionResetSettings);

            // Operating system
            RegisterCommand("(?:type|write) (.+)", ActionType);
            RegisterCommand("press (?:the )?enter(?: key)?", ActionPressEnter);
            RegisterCommand("press (?:the )?(?:space|space bar)(?: key)?", ActionPressSpace);
            RegisterCommand("scroll down|press page down", ActionScrollDown);
            RegisterCommand("scroll up|press page up", ActionScrollUp);
            RegisterCommand("close (?:(?:the(?: active)?|this) )?window", ActionCloseWindow);
            RegisterCommand("open (?:(?:my|the) )?(documents|music|pictures|videos|downloads|desktop)(?: (?:directory|folder|library))?", ActionOpenUserDirectory);
            RegisterCommand("open (?:the )?calculator", ActionOpenCalc);
            RegisterCommand("open (?:the )?(?:MS |ms)?paint", ActionOpenPaint);

            // Chess
            RegisterCommand("(?:make a )?move from ([A-H][1-8]) to ([A-H][1-8])", x =>
            {
                Speak($"Making a chess move from {x[0]} to {x[1]}");
                System.IO.File.AppendAllText("../Chess/log.txt", x[0] + x[1]);
            });

            RegisterCommand("(?:open|start) chess", x =>
            {
                Speak("Opening chess");
                Process.Start(new ProcessStartInfo("Chess.exe") { WorkingDirectory = "../Chess/" });
            });

            RegisterCommand("(?:close|stop) chess", x =>
            {
                Speak("Closing chess");
                foreach (Process proc in Process.GetProcessesByName("Chess"))
                {
                    proc.CloseMainWindow();
                }
            });

            // Problem solving
            RegisterCommand("search (?:for )?(.+?)(?: (?:using|on) (Google|YouTube|Wikipedia))?", ActionSearch);
            RegisterCommand("open weather forecast", ActionOpenWeatherForecast);
            RegisterCommand(@"how much is (\d+(?:.\d+)?) (\+|-|\*|/) (\d+(?:.\d+)?)", ActionSolveMathProblem);
            RegisterCommand("how much is (.+)", ActionGoogleMathProblem);
            RegisterCommand("play (?:me )?(?:some(?:thing from)? (.+?)|(?:a )?(.+?) (?:YouTube )?mix)(?: on YouTube)?", ActionPlayYouTubeMix);
            RegisterCommand("(?:what is|what's) the time|what time is it", ActionReadTime);
            RegisterCommand("(?:tell|read) (?:me )?a joke", ActionReadJoke);

            // Browser
            RegisterCommand("open (?:a )?new tab", ActionBrowserNewTab, AssistantMode.Browser);
            RegisterCommand("close (?:(?:the(?: active)?|this) )?tab", ActionBrowserCloseTab, AssistantMode.Browser);
            RegisterCommand("(?:re)?open (?:the (?:last )?)?(?:closed )?tab", ActionBrowserReopenTab, AssistantMode.Browser);
            RegisterCommand("switch (?:to (?:the )?next )?tab", ActionBrowserNextTab, AssistantMode.Browser);
            RegisterCommand("switch to (?:the )?previous tab", ActionBrowserPreviousTab, AssistantMode.Browser);
        }

        private void OfflineRecognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence >= 1d - Properties.Settings.Default.ActivationSensitivity)
            {
                Listen();
            }
        }

        private void RegisterCommand(string regex, Action<string[]> action, AssistantMode mode = AssistantMode.All)
        {
            cmdList.AddCommand(new VoiceCommand(regex, action, mode));
        }

        public async void Listen()
        {
            // Disable the offline speech recognizer
            offlineRecognizer.RecognizeAsyncCancel();
            // Switch the UI to listening mode
            updateListeningUI(true);

            // Let the user know the assistant is listening
            setRecognitionLabelText("Listening...");
            Speak("How can I help you?");

            // Listen and recognize
            await GoogleSpeechRecognizer.StreamingMicRecognizeAsync(
                HandleCommand,
                x => setRecognitionLabelText(x)
                );

            // Switch the UI back to idle mode
            updateListeningUI(false);
            // Re-enable the offline speech recognizer
            ActivateOfflineRecognizer();
        }

        private void LoadAssistantName()
        {
            string name = Properties.Settings.Default.Name;

            // Stop offline speech recognizer and restart it with the new name
            offlineRecognizer.RecognizeAsyncCancel();
            offlineRecognizer.UnloadAllGrammars();
            offlineRecognizer.LoadGrammar(new Grammar(new Choices(name).ToGrammarBuilder()));

            // Display the current name of the assistant
            setNameLabelText("Name: " + name.CapitalizeFirstLetter());
        }

        private void ActivateOfflineRecognizer() => offlineRecognizer.RecognizeAsync(RecognizeMode.Multiple);

        private void Speak(string text)
        {
            appendLog("[Assistant] " + text);
            synthesizer.SpeakAsync(text);
        }

        private void HandleCommand(string cmd)
        {
            appendLog("[User] " + cmd);
            cmdList.HandleInput(cmd, GetCurrentAssistantMode());
        }

        private void TypeText(string text)
        {
            if (text == null || text == string.Empty)
            {
                return;
            }

            //SendKeys.SendWait(text);

            invokeDispatcher(() =>
            {
                // Get the current clipboard data
                string oldText = Clipboard.GetText();
                Image oldImage = Clipboard.GetImage();

                // Set the clipboard to the text to type
                Clipboard.SetText(text);

                // Press Ctrl+V to paste the text in the clipboard
                //Windows.Keyboard.Down(Key.LeftCtrl);
                //Windows.Keyboard.Press(Key.V);
                //Windows.Keyboard.Up(Key.LeftCtrl);
                SendKeys.SendWait("^{v}");

                // Set the clipboard data back to the original data
                if (oldText != null && oldText != string.Empty)
                {
                    Clipboard.SetText(oldText);
                }

                if (oldImage != null)
                {
                    Clipboard.SetImage(oldImage);
                }
            });
        }

        private void SendKeysWait(string keys) => invokeDispatcher(() => SendKeys.SendWait(keys));

        private AssistantMode GetCurrentAssistantMode()
        {
            Process proc = Windows.GetForegroundProcess();
            string procName = proc.ProcessName;
            string windowTitle = proc.MainWindowTitle;

            switch (procName)
            {
                case "chrome":
                    return AssistantMode.Browser;

                default:
                    return AssistantMode.Default;
            }
        }
    }
}
