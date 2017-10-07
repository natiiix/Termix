using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Google.Cloud.Speech.V1;

namespace Termix
{
    public partial class MainWindow : Window
    {
        private Handler handler;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await StreamingMicRecognizeAsync(10);
            return;

            handler = new Handler();
            listBoxCommands.Items.Clear();
            listBoxCommands.Items.Add("Hello");
            listBoxCommands.Items.Add("World");
            listBoxCommands.Items.Add("Yay!");
        }

        private void ListBoxCommands_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBoxCommands.SelectedItem != null)
            {
                handler.Handle(listBoxCommands.SelectedItem as string);
                listBoxCommands.SelectedIndex = -1;
            }
        }

        private async Task<object> StreamingMicRecognizeAsync(int seconds)
        {
            int SAMPLE_RATE = 16000;

            if (NAudio.Wave.WaveIn.DeviceCount < 1)
            {
                Console.WriteLine("No microphone!");
                return -1;
            }

            SpeechClient speech = SpeechClient.Create();
            SpeechClient.StreamingRecognizeStream streamingCall = speech.StreamingRecognize();

            // Write the initial request with the config.
            await streamingCall.WriteAsync(new StreamingRecognizeRequest()
            {
                StreamingConfig = new StreamingRecognitionConfig()
                {
                    Config = new RecognitionConfig()
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                        SampleRateHertz = SAMPLE_RATE,
                        LanguageCode = "en",
                    },
                    InterimResults = true,
                }
            });

            // Read from the microphone and stream to API.
            NAudio.Wave.WaveInEvent waveIn = new NAudio.Wave.WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new NAudio.Wave.WaveFormat(SAMPLE_RATE, 1)
            };

            object writeLock = new object();
            bool writeMore = true;

            // Print responses as they arrive.
            Task printResponses = Task.Run(async () =>
            {
                while (await streamingCall.ResponseStream.MoveNext(default(System.Threading.CancellationToken)))
                {
                    foreach (StreamingRecognitionResult result in streamingCall.ResponseStream.Current.Results)
                    {
                        foreach (SpeechRecognitionAlternative alternative in result.Alternatives)
                        {
                            Dispatcher?.Invoke(() => listBoxCommands.Items.Add("Final: " + result.IsFinal.ToString() + " - " + alternative.Transcript));
                        }

                        if (result.IsFinal)
                        {
                            Dispatcher?.Invoke(() => listBoxCommands.Items.Add("--------- Final Result --------"));

                            // TODO: Some kind of a callback to return the final string

                            //waveIn.StopRecording();

                            //lock (writeLock)
                            //    writeMore = false;
                        }
                    }
                }
            });

            waveIn.DataAvailable += (object sender, NAudio.Wave.WaveInEventArgs args) =>
                {
                    lock (writeLock)
                    {
                        if (!writeMore)
                            return;

                        streamingCall.WriteAsync(new StreamingRecognizeRequest()
                        {
                            AudioContent = Google.Protobuf.ByteString.CopyFrom(args.Buffer, 0, args.BytesRecorded)
                        }).Wait();
                    }
                };

            waveIn.StartRecording();
            Console.WriteLine("Speak now.");

            await Task.Delay(TimeSpan.FromSeconds(seconds));
            Dispatcher?.Invoke(() => listBoxCommands.Items.Add("--------- Timed Out --------"));

            // Stop recording and shut down.
            waveIn.StopRecording();

            lock (writeLock)
                writeMore = false;

            await streamingCall.WriteCompleteAsync();
            await printResponses;

            return 0;
        }
    }
}