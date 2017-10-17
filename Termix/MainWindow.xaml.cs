using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Google.Cloud.Speech.V1;
using System.Diagnostics;
using System.Speech.Recognition;
using System.Speech.Synthesis;

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
            offlineRecognizer = new SpeechRecognitionEngine();
            offlineRecognizer.SpeechRecognized += OfflineRecognizer_SpeechRecognized;
            offlineRecognizer.SetInputToDefaultAudioDevice();
            SetAssistantName(DEFAULT_ASSISTANT_NAME);
            ActivateOfflineRecognizer();

            cmdList = new VoiceCommandList(x => MessageBox.Show("Unrecognized command: " + x));

            cmdList.AddCommand(new VoiceCommand(
                x => x.StartsWithCaseInsensitive("change name to "),
                x => x.Substring(15),
                x => SetAssistantName(x)
                ));

            cmdList.AddCommand(new VoiceCommand(
                x => x.EqualsCaseInsensitive("close yourself"),
                x => string.Empty,
                x => Dispatcher?.Invoke(Close)
                ));

            cmdList.AddCommand(new VoiceCommand(
                x => x.StartsWithCaseInsensitive("type "),
                x => x.Substring(5),
                System.Windows.Forms.SendKeys.SendWait
                ));

            cmdList.AddCommand(new VoiceCommand(
                x => x.StartsWithCaseInsensitive("search "),
                x => x.Substring(7),
                x => Process.Start("https://www.google.com/search?q=" + x.Replace(' ', '+'))
                ));

            cmdList.AddCommand(new VoiceCommand(
                x => x.EqualsCaseInsensitive("open weather forecast"),
                x => string.Empty,
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
        }

        private void ActivateOfflineRecognizer()
        {
            offlineRecognizer.RecognizeAsync(RecognizeMode.Multiple);
        }
    }
}