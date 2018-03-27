using Google.Cloud.Speech.V1;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Termix
{
    public static class GoogleSpeechRecognizer
    {
        // Frequency in Hz
        private const int RECOGNIZER_SAMPLE_RATE = 16000;

        // Recognition has just started, nothing has been recognized yet
        private static readonly TimeSpan RECOGNITION_TIMEOUT_INITIAL = TimeSpan.FromSeconds(10);

        // Part of the speech has already been recognized
        private static readonly TimeSpan RECOGNITION_TIMEOUT_PARTIAL = TimeSpan.FromSeconds(5);

        // Final form of a speech segment has been recognized
        private static readonly TimeSpan RECOGNITION_TIMEOUT_AFTER_FINAL = TimeSpan.FromSeconds(5);

        public static bool StopListening { get; set; } = false;

        public static async Task<int> StreamingMicRecognizeAsync(Action<string> finalRecognitionAction, Action<string> partialRecognitionAction)
        {
            DateTime dtTimeout = DateTime.Now + RECOGNITION_TIMEOUT_INITIAL;

            if (NAudio.Wave.WaveIn.DeviceCount < 1)
            {
                MessageBox.Show("No microphone!");
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
                        LanguageCode = "en-US",
                    },
                    InterimResults = true
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
                while (!StopListening && await streamingCall.ResponseStream.MoveNext(default(CancellationToken)))
                {
                    foreach (StreamingRecognitionResult result in streamingCall.ResponseStream.Current.Results)
                    {
                        foreach (SpeechRecognitionAlternative alternative in result.Alternatives)
                        {
                            string transcript = alternative.Transcript.TrimSpaces();

                            // Final recognition result
                            if (result.IsFinal)
                            {
                                dtTimeout = DateTime.Now + RECOGNITION_TIMEOUT_AFTER_FINAL;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                                Task.Factory.StartNew(() => finalRecognitionAction(transcript));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            }
                            // Recognition continues
                            else
                            {
                                dtTimeout = DateTime.Now + RECOGNITION_TIMEOUT_PARTIAL;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                                Task.Factory.StartNew(() => partialRecognitionAction(transcript));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            }
                        }
                    }
                }
            });

            // Audio recorded
            waveIn.DataAvailable += (object sender, NAudio.Wave.WaveInEventArgs args) =>
            {
                if (StopListening)
                {
                    return;
                }

                lock (writeLock)
                {
                    if (!writeMore)
                    {
                        return;
                    }

                    // Timed out
                    if (DateTime.Now >= dtTimeout)
                    {
                        // Stop recording
                        waveIn.StopRecording();
                        writeMore = false;
                        return;
                    }

                    try
                    {
                        streamingCall.WriteAsync(new StreamingRecognizeRequest()
                        {
                            AudioContent = Google.Protobuf.ByteString.CopyFrom(args.Buffer, 0, args.BytesRecorded)
                        }).Wait();
                    }
                    catch { }
                }
            };

            // Start recording
            waveIn.StartRecording();

            // Wait for the recording to stop
            while (!StopListening && writeMore)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            // Shut down
            await streamingCall.WriteCompleteAsync();
            await printResponses;

            StopListening = false;
            return 0;
        }
    }
}
