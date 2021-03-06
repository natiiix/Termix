﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Windows.Forms;

namespace Termix
{
    public partial class VoiceAssistant
    {
        private const string NUMBERS = @"zero|one|two|three|four|five|six|seven|eight|nine|ten|\d+";
        private const string ART = "(?:(?:a|the) )?"; // Optional definite/indefinite article followed by a space
        private const string ART_ONLY = "a|the"; // Only the definite / indefinite article (used when there are more options)
        private readonly static string[] PROMPTS = { "How can I help you?", "How may I help you?", "How can I assist you?", "How may I assist you?", "What can I do for you?" };
        private readonly static string DATA_DIR = AppDomain.CurrentDomain.BaseDirectory + @"data\";

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
                invokeDispatcher(() => MessageBox.Show(ex.Message, "Error (Invalid Data)"));
            }

            // User commands
            foreach (DataAlias cmd in data.UserCommands)
            {
                RegisterCommand(cmd.Pattern, args =>
                {
                    try
                    {
                        Speak("Executing a user command");
                        Process.Start(cmd.Value);
                    }
                    catch (Exception ex)
                    {
                        invokeDispatcher(() => MessageBox.Show(ex.Message, "Error (Invalid User Command)"));
                    }
                });
            }

            // Assistant
            RegisterCommand("do nothing|don't do anything|stop listening|never mind|nevermind", ActionStopListening);
            RegisterCommand($"(?:close|shut down) (?:yourself|{ART} assistant)|shut (?:(?:yourself|{ART} assistant) )?down|shut it down", ActionAssistantShutDown);
            RegisterCommand($"(?:change (?:(?:your|{ART_ONLY}) )?(?:name|activation command)|rename(?: yourself)?) to (.+)", ActionAssistantRename);
            RegisterCommand($@"(increase|decrease) {ART}(?:voice )?activation(?: command)? sensitivity(?: by (\d+(?:.\d+|%)?))?", ActionChangeActivationSensitivity);
            RegisterCommand($"(enable|disable) {ART}(?:(?:voice )?feedback|speech synthesis)", ActionSetVoiceFeedback);
            RegisterCommand($"reset {ART}(?:assistant )?(?:settings|options|configuration)", ActionResetSettings);

            // Operating system
            RegisterCommand($"close (?:{ART}(?:active )?|this )?window", ActionCloseWindow);
            RegisterCommand($"open (?:(?:my|{ART_ONLY}) )?(documents|music|pictures|videos|downloads|desktop)(?: (?:directory|folder|library))?", ActionOpenUserDirectory);
            RegisterCommand($"open {ART}notepad", ActionOpenNotepad);
            RegisterCommand($"open {ART}calculator", ActionOpenCalc);
            RegisterCommand($"open {ART}(?:Microsoft |ms ?)?paint", ActionOpenPaint);
            RegisterCommand($"take {ART}screenshot", ActionScreenshot);

            // Volume
            RegisterCommand($"(unmute|mute|enable|disable) (?:all )?(?:of )?{ART}(?:system )?(?:sounds? volume|sounds?|volume)", ActionMuteSound);
            RegisterCommand($"(?:change|set) {ART}(?:system )?(?:playback )?(?:sound )?volume to ({NUMBERS}) ?(?:%|percent)?", ActionSetVolume);
            RegisterCommand($"(increase|decrease) {ART}(?:system )?(?:playback )?(?:sound )?volume(?: by ({NUMBERS}) ?(?:%|percent)?)?", ActionChangeVolume);

            // Keyboard
            if (data.Aliases.Length > 0)
            {
                string listOfAlternatives = string.Join("|", data.Aliases.Select(x => string.Join("|", x.Pattern)));
                RegisterCommand($"(?:type|write|enter)(?: (?:my|{ART_ONLY}))?? ({listOfAlternatives})", ActionEnterData);
            }

            RegisterCommand("(?:type|write) ?(.+)", ActionType);
            RegisterCommand("scroll down", ActionScrollDown);
            RegisterCommand("scroll up", ActionScrollUp);
            RegisterCommand($@"press {ART}(.+?)(?: key)?(?: ({NUMBERS}) (?:times|\*))?", ActionPressKey);
            RegisterCommand($"select all(?:{ART} text)?", ActionSelectAll);
            RegisterCommand($"(copy|cut|paste)(?:(?: (?:to|into|from))? {ART}clipboard)?", ActionClipboard);
            RegisterCommand($"delete {ART}(?:(?:last|previous) )?(?:({NUMBERS}) )?words?", ActionDeleteWord);
            RegisterCommand($"send {ART}message", ActionSendMessage, AssistantMode.Messenger);

            // Mouse
            RegisterCommand($"move {ART}(?:mouse(?: cursor)?|cursor) ({NUMBERS}) pixels (?:to {ART})?(left|right|up|down)", ActionMoveCursor);
            RegisterCommand($@"(?:(?:do|perform) {ART})?(left|right|middle) (?:mouse )?click(?: ({NUMBERS}) (?:times|\*))?", ActionMouseClick);

            // Problem solving - offline
            RegisterCommand(@"how much is (-?\d+(?:.\d+)?) (\+|-|\*|/) (-?\d+(?:.\d+)?)", ActionSolveMathProblem);
            RegisterCommand("(?:what is|what's) the time|what time is it", ActionReadTime);
            RegisterCommand($"(?:tell|read) (?:me )?{ART} joke", ActionReadJoke);

            // Problem solving - online
            RegisterCommand($"(?:open(?: up)?|show me|display) {ART}weather forecast", ActionOpenWeatherForecast);
            RegisterCommand("how much is (.+)", ActionGoogleMathProblem);
            RegisterCommand($"(?:search(?: for)?|find|show me) (.+?)(?: (?:using|on) {ART}(Google|YouTube|Wikipedia))?", ActionSearch);
            RegisterCommand($"play (?:me )?(?:some(?:thing from)? (.+?)|{ART}(.+?) (?:YouTube )?mix)(?: on YouTube)?(?: for me)?", ActionPlayYouTubeMix);
            RegisterCommand($"play (?:{ART}(?:YouTube )?video called )?(.+?)(?: video)?(?: on YouTube)?", ActionPlayYouTubeVideo);

            if (data.FacebookContacts.Length > 0)
            {
                string listOfAlternatives = string.Join("|", data.FacebookContacts.Select(x => string.Join("|", x.Pattern)));
                RegisterCommand($"open(?: my)?(?: Facebook)? chat with(?: (?:{ART_ONLY}|my))? ({listOfAlternatives})(?: on Facebook)?(?: in Messenger)?", ActionOpenFacebookChat);
            }

            // Browser
            RegisterCommand($"open (?:(?:(?:{ART_ONLY}|my) )?(?:web )?browser|{ART}(?:new )?(?:web )?browser window)", ActionOpenWebBrowser);
            RegisterCommand($"open {ART}(Google|YouTube|Wikipedia|Facebook|Twitter|Twitch|Tumblr|Discord|GitHub)(?: in {ART}new (?:browser )?(?:tab|window))?", ActionOpenWebpage);
            RegisterCommand($"open {ART}new tab", ActionBrowserNewTab, AssistantMode.Browser);
            RegisterCommand($"close (?:{ART}(?:active )?|this )?tab", ActionBrowserCloseTab, AssistantMode.Browser);
            RegisterCommand($"(?:re)?open {ART}(?:last )?(?:closed )?tab", ActionBrowserReopenTab, AssistantMode.Browser);
            RegisterCommand($"switch (?:to )?{ART}(?:next )?tab", ActionBrowserNextTab, AssistantMode.Browser);
            RegisterCommand($"switch to {ART}previous tab", ActionBrowserPreviousTab, AssistantMode.Browser);
            RegisterCommand($"(?:go|move|switch) (?:back|(?:back )?to {ART}(?:previous|last) page)", ActionBrowserBack, AssistantMode.Browser);
            RegisterCommand($"(?:go|move|switch) (?:forward|(?:forward )?to {ART}(?:next|following) page)", ActionBrowserForward, AssistantMode.Browser);

            // Chess
            RegisterCommand($"(?:make {ART}(?:chess )?)?move from ([A-H][1-8]) to ([A-H][1-8])", x =>
            {
                Speak($"Making a chess move from {x[0]} to {x[1]}");
                System.IO.File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + @"..\Chess\log.txt", x[0] + x[1]);
            });

            RegisterCommand("(?:open|start) chess", x =>
            {
                Speak("Opening chess");
                Process.Start(new ProcessStartInfo("Chess.exe") { WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory + @"..\Chess\" });
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
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
                System.Threading.Thread.Sleep(100);

                // Press Ctrl+V to paste the text in the clipboard
                WinApi.Keyboard.Down(System.Windows.Input.Key.LeftCtrl);
                System.Threading.Thread.Sleep(20);
                WinApi.Keyboard.Press(System.Windows.Input.Key.V);
                System.Threading.Thread.Sleep(20);
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
