using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Windows.Forms;
using System.Net.Http;

namespace Termix
{
    public partial class VoiceAssistant
    {
        private const string NUMBERS = @"zero|one|two|three|four|five|six|seven|eight|nine|ten|\d+";
        private readonly static string[] PROMPTS = { "How can I help you?", "How may I help you?", "How can I assist you?", "How may I assist you?", "What can I do for you?" };
        private readonly static string DATA_DIR = AppDomain.CurrentDomain.BaseDirectory + "/data/";

        private SpeechRecognitionEngine offlineRecognizer;
        private SpeechSynthesizer synthesizer;
        private HttpClient httpClient;
        private VoiceCommandList cmdList;
        private AssistantData data;

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

            try
            {
                data = new AssistantData(DATA_DIR);
            }
            catch (ArgumentException ex)
            {
                invokeDispatcher(() =>
                {
                    MessageBox.Show(ex.Message, "Error: Invalid Data");
                });
            }

            // Assistant
            RegisterCommand("do nothing|don't do anything|stop listening|never mind|nevermind", ActionStopListening);
            RegisterCommand("close (?:yourself|the assistant)|shut (?:(?:yourself|the assistant) )?down|shut it down", ActionAssistantShutDown);
            RegisterCommand("(?:change (?:(?:your|the) )?(?:name|activation command)|rename(?: yourself)?) to (.+)", ActionAssistantRename);
            RegisterCommand(@"(increase|decrease) (?:the )?(?:voice )?activation(?: command)? sensitivity(?: by (\d+(?:.\d+|%)?))?", ActionChangeActivationSensitivity);
            RegisterCommand("(enable|disable) (?:the )?(?:(?:voice )?feedback|speech synthesis)", ActionSetVoiceFeedback);
            RegisterCommand("reset (?:the )?assistant (?:settings|options|configuration)", ActionResetSettings);

            // Operating system
            RegisterCommand("close (?:(?:the(?: active)?|this) )?window", ActionCloseWindow);
            RegisterCommand("open (?:(?:my|the) )?(documents|music|pictures|videos|downloads|desktop)(?: (?:directory|folder|library))?", ActionOpenUserDirectory);
            RegisterCommand("open (?:the )?calculator", ActionOpenCalc);
            RegisterCommand("open (?:the )?(?:MS |ms)?paint", ActionOpenPaint);
            RegisterCommand("open (?:(?:(?:the|a|my) )?(?:web )?browser|(?:a )?(?:new )?(?:web )?browser window)", ActionOpenWebBrowser);
            RegisterCommand("open (Google|YouTube|Wikipedia|Facebook|Twitter|Twitch|Tumblr|Discord|GitHub)", ActionOpenWebpage);

            // Volume
            RegisterCommand("(unmute|mute|enable|disable)(?: all)?(?: of)?(?: the)?(?: system)?(?: sounds?)?(?: volume)?", ActionMuteSound);
            RegisterCommand($"(?:change|set) (?:the )?(?:system )?(?:playback )?(?:sound )?volume to ({NUMBERS}) ?(?:%|percent)?", ActionSetVolume);
            RegisterCommand($"(increase|decrease) (?:the )?(?:system )?(?:playback )?(?:sound )?volume(?: by ({NUMBERS}) ?(?:%|percent)?)?", ActionChangeVolume);

            // Keyboard
            if (data.Aliases.Length > 0)
            {
                string listOfAlternatives = string.Join("|", data.Aliases.Select(x => string.Join("|", x.Alternatives)));
                RegisterCommand($"(?:type|write|enter)(?: (?:my|the))?? ({listOfAlternatives})", ActionEnterData);
            }

            RegisterCommand("(?:type|write)(?: (?:the )?(?:following sentence|following|sentence))? (.+)", ActionType);
            RegisterCommand("scroll down", ActionScrollDown);
            RegisterCommand("scroll up", ActionScrollUp);
            RegisterCommand($@"press (?:the )?(.+?)(?: key)?(?: ({NUMBERS}) (?:times|\*))?", ActionPressKey);
            RegisterCommand("select all(?: the)?(?: text)?", ActionSelectAll);
            RegisterCommand("(copy|cut|paste)(?:(?: (?:to|into|from))? clipboard)?", ActionClipboard);
            RegisterCommand("send(?: the)? message", ActionSendMessage, AssistantMode.Messenger);

            // Mouse
            RegisterCommand($"move (?:the )?(?:mouse(?: cursor)?|cursor) ({NUMBERS}) pixels (?:to the )?(left|right|up|down)", ActionMoveCursor);
            RegisterCommand($@"(?:(?:do|perform) (?:a )?)?(left|right|middle) (?:mouse )?click(?: ({NUMBERS}) (?:times|\*))?", ActionMouseClick);

            // Problem solving - offline
            RegisterCommand(@"how much is (\d+(?:.\d+)?) (\+|-|\*|/) (\d+(?:.\d+)?)", ActionSolveMathProblem);
            RegisterCommand("(?:what is|what's) the time|what time is it", ActionReadTime);
            RegisterCommand("(?:tell|read) (?:me )?a joke", ActionReadJoke);
            RegisterCommand("take(?: a)? screenshot", ActionScreenshot);

            // Problem solving - online
            RegisterCommand("(?:open(?: up)?|show me|display) (?:the )?weather forecast", ActionOpenWeatherForecast);
            RegisterCommand("how much is (.+)", ActionGoogleMathProblem);
            RegisterCommand("(?:search(?: for)?|find|show me) (.+?)(?: (?:using|on) (Google|YouTube|Wikipedia))?", ActionSearch);
            RegisterCommand("play (?:me )?(?:some(?:thing from)? (.+?)|(?:a )?(.+?) (?:YouTube )?mix)(?: on YouTube)?", ActionPlayYouTubeMix);
            RegisterCommand("play (?:(?:a )?(?:YouTube )?video called )?(.+?)(?: video)?(?: on YouTube)?", ActionPlayYouTubeVideo);

            if (data.FacebookContacts.Length > 0)
            {
                string listOfAlternatives = string.Join("|", data.FacebookContacts.Select(x => string.Join("|", x.Alternatives)));
                RegisterCommand($"open(?: my)?(?: Facebook)? chat with(?: (?:the|my))? ({listOfAlternatives})(?: on Facebook)?(?: in Messenger)?", ActionOpenFacebookChat);
            }

            // Browser
            RegisterCommand("open (?:a )?new tab", ActionBrowserNewTab, AssistantMode.Browser);
            RegisterCommand("close (?:(?:the(?: active)?|this) )?tab", ActionBrowserCloseTab, AssistantMode.Browser);
            RegisterCommand("(?:re)?open (?:the (?:last )?)?(?:closed )?tab", ActionBrowserReopenTab, AssistantMode.Browser);
            RegisterCommand("switch (?:to (?:the )?next )?tab", ActionBrowserNextTab, AssistantMode.Browser);
            RegisterCommand("switch to (?:the )?previous tab", ActionBrowserPreviousTab, AssistantMode.Browser);
            RegisterCommand("go (?:back|to (?:the )(?:previous|last) page)", ActionBrowserBack, AssistantMode.Browser);
            RegisterCommand("go (?:forward|to (?:the )next page)", ActionBrowserForward, AssistantMode.Browser);

            // Chess
            RegisterCommand("(?:make a (?:chess )?)?move from ([A-H][1-8]) to ([A-H][1-8])", x =>
            {
                Speak($"Making a chess move from {x[0]} to {x[1]}");
                System.IO.File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "/../Chess/log.txt", x[0] + x[1]);
            });

            RegisterCommand("(?:open|start) chess", x =>
            {
                Speak("Opening chess");
                Process.Start(new ProcessStartInfo("Chess.exe") { WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory + "/../Chess/" });
            });

            RegisterCommand("(?:close|stop) chess", x =>
            {
                Speak("Closing chess");
                foreach (Process proc in Process.GetProcessesByName("Chess"))
                {
                    proc.CloseMainWindow();
                }
            });
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

        public void Listen()
        {
            const string LISTENING_TEXT = "Listening...";

            // Disable the offline speech recognizer
            offlineRecognizer.RecognizeAsyncCancel();
            // Switch the UI to listening mode
            updateListeningUI(true);

            // Let the user know the assistant is listening
            setRecognitionLabelText(LISTENING_TEXT);
            Speak(PROMPTS[new Random().Next(PROMPTS.Length)]);

            // Listen and recognize
            GoogleSpeechRecognizer.StreamingMicRecognizeAsync(
                x =>
                {
                    string name = Properties.Settings.Default.Name;

                    if (x.ToLower().StartsWith(name.ToLower()))
                    {
                        HandleCommand(x.Substring(name.Length).TrimSpaces());
                    }
                    else
                    {
                        HandleCommand(x);
                    }

                    setRecognitionLabelText(LISTENING_TEXT);
                },
                x => setRecognitionLabelText(x)
            ).ContinueWith(_ =>
            {
                // Switch the UI back to idle mode
                updateListeningUI(false);
                // Re-enable the offline speech recognizer
                ActivateOfflineRecognizer();
            });
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

            if (Properties.Settings.Default.VoiceFeedback)
            {
                synthesizer.SpeakAsync(text);
            }
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
                WinApi.Keyboard.Down(System.Windows.Input.Key.LeftCtrl);
                System.Threading.Thread.Sleep(10);
                WinApi.Keyboard.Press(System.Windows.Input.Key.V);
                System.Threading.Thread.Sleep(10);
                WinApi.Keyboard.Up(System.Windows.Input.Key.LeftCtrl);

                // Alternative (old) way to press Ctrl+V.
                // Some applications (Facebook Messenger) seem to ignore
                // this method of pressing Ctrl+V.
                // Presumably because the keys are being pressed too briefly.
                //SendKeys.SendWait("^{v}");

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
            Process proc = WinApi.GetForegroundProcess();
            string procName = proc.ProcessName;
            string windowTitle = proc.MainWindowTitle;

            AssistantMode mode = AssistantMode.Default;

            if (procName == "chrome")
            {
                mode |= AssistantMode.Browser;
            }

            if (windowTitle == "Messenger - Google Chrome")
            {
                mode |= AssistantMode.Messenger;
            }

            return mode;
        }
    }
}
