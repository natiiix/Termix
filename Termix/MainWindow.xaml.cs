using System.Windows;
using System.Diagnostics;
using System.Speech.Recognition;

namespace Termix
{
    public partial class MainWindow : Window
    {
        private const string DEFAULT_ASSISTANT_NAME = "Assistant";
        private SpeechRecognitionEngine offlineRecognizer;
        private VoiceCommandList cmdList;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //bool asdf = ExpressionComparator.Compare("change your activation to John", "change [your] { name | activation [ command ] } to *", out string asdsffa);

            offlineRecognizer = new SpeechRecognitionEngine();
            offlineRecognizer.SpeechRecognized += OfflineRecognizer_SpeechRecognized;
            offlineRecognizer.SetInputToDefaultAudioDevice();
            SetAssistantName(DEFAULT_ASSISTANT_NAME);
            ActivateOfflineRecognizer();

            cmdList = new VoiceCommandList(x => MessageBox.Show("Unrecognized command: " + x));

            cmdList.AddCommand(new VoiceCommand(
                "change [your] { name | activation [ command ] } to *",
                x => Dispatcher?.Invoke(() => SetAssistantName(x))
                ));

            cmdList.AddCommand(new VoiceCommand(
                "close yourself",
                x => Dispatcher?.Invoke(Close)
                ));

            cmdList.AddCommand(new VoiceCommand(
                "{ type | write } *",
                System.Windows.Forms.SendKeys.SendWait
                ));

            cmdList.AddCommand(new VoiceCommand(
                "search [for] *",
                x => Process.Start("https://www.google.com/search?q=" + x.Replace(' ', '+'))
                ));

            cmdList.AddCommand(new VoiceCommand(
                "open weather forecast",
                x => Process.Start("https://www.google.com/search?q=weather+forecast")
                ));

            listBoxCommandList.Items.Add("Available");
            listBoxCommandList.Items.Add("Commands");
            listBoxCommandList.Items.Add("Go");
            listBoxCommandList.Items.Add("Here");
        }

        private void OfflineRecognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Listen();
        }

        private void ButtonListen_Click(object sender, RoutedEventArgs e)
        {
            Listen();
        }

        private async void Listen()
        {
            offlineRecognizer.RecognizeAsyncCancel();
            Dispatcher?.Invoke(() => UpdateListeningUI(true));

            labelRealtimeRecognition.Content = "Listening...";

            // Listen and recognize
            await GoogleSpeechRecognizer.StreamingMicRecognizeAsync(
                x => cmdList.HandleInput(x),
                x => Dispatcher?.Invoke(() => labelRealtimeRecognition.Content = x)
                );

            Dispatcher?.Invoke(() => UpdateListeningUI(false));
            ActivateOfflineRecognizer();
        }

        private void UpdateListeningUI(bool listening)
        {
            // The button can only be used when not listening
            buttonListen.IsEnabled = !listening;

            // If listening
            if (listening)
            {
                // Hide the "Listen button"
                buttonListen.Visibility = Visibility.Hidden;
                // Display the real-time recognition label
                labelRealtimeRecognition.Visibility = Visibility.Visible;
            }
            // If not listening
            else
            {
                // Display the "Listen" button
                buttonListen.Visibility = Visibility.Visible;
                // Hide the real-time recognition label
                labelRealtimeRecognition.Visibility = Visibility.Hidden;
                // Clear the real-time-recognition label
                labelRealtimeRecognition.ClearValue(ContentProperty);
            }
        }

        private void SetAssistantName(string name)
        {
            offlineRecognizer.RecognizeAsyncCancel();
            offlineRecognizer.UnloadAllGrammars();
            offlineRecognizer.LoadGrammar(new Grammar(new Choices(name).ToGrammarBuilder()));

            labelName.Content = "Name: " + name.CapitalizeFirstLetter();
        }

        private void ActivateOfflineRecognizer()
        {
            offlineRecognizer.RecognizeAsync(RecognizeMode.Multiple);
        }
    }
}