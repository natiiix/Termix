using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Google.Cloud.Speech.V1;

namespace Termix
{
    public partial class MainWindow : Window
    {
        private const int RECOGNIZER_SAMPLE_RATE = 16000;
        private readonly TimeSpan RECOGNITION_TIMEOUT_INITIAL = TimeSpan.FromSeconds(5);
        private readonly TimeSpan RECOGNITION_TIMEOUT_AFTER_FINAL = TimeSpan.FromSeconds(3);

        private Handler handler;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await StreamingMicRecognizeAsync();

            handler = new Handler();
            //listBoxCommands.Items.Clear();
            listBoxCommands.Items.Add("Done!");
        }

        private void ListBoxCommands_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBoxCommands.SelectedItem != null)
            {
                handler.Handle(listBoxCommands.SelectedItem as string);
                listBoxCommands.SelectedIndex = -1;
            }
        }

        private async Task<object> StreamingMicRecognizeAsync()
        {
            DateTime dtTimeout = DateTime.Now + RECOGNITION_TIMEOUT_INITIAL;

            if (NAudio.Wave.WaveIn.DeviceCount < 1)
            {
                Console.WriteLine("No microphone!");
                return -1;
            }

            SpeechClient speech = SpeechClient.Create();
            SpeechClient.StreamingRecognizeStream streamingCall = speech.StreamingRecognize();

            // Write the initial request with the config
            await streamingCall.WriteAsync(new StreamingRecognizeRequest()
            {
                StreamingConfig = new StreamingRecognitionConfig()
                {
                    Config = new RecognitionConfig()
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                        SampleRateHertz = RECOGNIZER_SAMPLE_RATE,
                        LanguageCode = "en",
                    },
                    InterimResults = true,
                }
            });

            // Read from the microphone and stream to API
            NAudio.Wave.WaveInEvent waveIn = new NAudio.Wave.WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new NAudio.Wave.WaveFormat(RECOGNIZER_SAMPLE_RATE, 1)
            };

            object writeLock = new object();
            bool writeMore = true;

            // Print responses as they arrive
            Task printResponses = Task.Run(async () =>
            {
                while (await streamingCall.ResponseStream.MoveNext(default(System.Threading.CancellationToken)))
                {
                    foreach (StreamingRecognitionResult result in streamingCall.ResponseStream.Current.Results)
                    {
                        string strRecognized = string.Empty;

                        foreach (SpeechRecognitionAlternative alternative in result.Alternatives)
                        {
                            strRecognized += alternative.Transcript + " / ";
                        }

                        strRecognized += result.Alternatives.Count.ToString();

                        // Final recognition result
                        if (result.IsFinal)
                        {
                            // TODO: Implement a callback to return the final string
                            dtTimeout = DateTime.Now + RECOGNITION_TIMEOUT_AFTER_FINAL;
                            Dispatcher?.Invoke(() => listBoxCommands.Items.Add(strRecognized));
                        }
                        // Recognition continues
                        else
                        {
                            dtTimeout = DateTime.MaxValue;
                            Dispatcher?.Invoke(() => labelRealtimeRecognition.Content = strRecognized);
                        }
                    }
                }
            });

            // Audio recorded
            waveIn.DataAvailable += (object sender, NAudio.Wave.WaveInEventArgs args) =>
                {
                    lock (writeLock)
                    {
                        if (!writeMore)
                            return;

                        // Timed out
                        if (DateTime.Now >= dtTimeout)
                        {
                            // Stop recording
                            waveIn.StopRecording();
                            writeMore = false;
                            Dispatcher?.Invoke(() => listBoxCommands.Items.Add("--------- Timed Out --------"));
                            return;
                        }

                        streamingCall.WriteAsync(new StreamingRecognizeRequest()
                        {
                            AudioContent = Google.Protobuf.ByteString.CopyFrom(args.Buffer, 0, args.BytesRecorded)
                        }).Wait();
                    }
                };

            waveIn.StartRecording();
            Console.WriteLine("Speak now.");

            // Wait for the recording to stop
            while (writeMore)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            // Shut down
            await streamingCall.WriteCompleteAsync();
            await printResponses;

            return 0;
        }
    }
}