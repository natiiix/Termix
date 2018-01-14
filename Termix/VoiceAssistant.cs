﻿using System;
using System.Speech.Recognition;
using System.Windows;

namespace Termix
{
    public partial class VoiceAssistant
    {
        private const string DEFAULT_ASSISTANT_NAME = "Assistant";

        private string assistantName;
        private SpeechRecognitionEngine offlineRecognizer;
        private VoiceCommandList cmdList;

        // Invoke dispatcher
        public delegate void InvokeDispatcherCallback(Action action);

        private InvokeDispatcherCallback invokeDispatcher;

        // Close main window
        public delegate void CloseMainWindowCallback();

        private CloseMainWindowCallback closeMainWindow;

        // Add command to list
        public delegate void AddCommandToListCallback(string strCommand);

        private AddCommandToListCallback addCommandToList;

        // Set recognition label text
        public delegate void SetRecognitionLabelTextCallback(string strText);

        private SetRecognitionLabelTextCallback setRecognitionLabelText;

        // Set name label text
        public delegate void SetNameLabelTextCallback(string strName);

        private SetNameLabelTextCallback setNameLabelText;

        // Update listening UI
        public delegate void UpdateListeningUICallback(bool listeningInProgress);

        private UpdateListeningUICallback updateListeningUI;

        // Constructor
        public VoiceAssistant(
            InvokeDispatcherCallback invokeDispatcherCallback,
            CloseMainWindowCallback closeMainWindowCallback,
            AddCommandToListCallback addCommandToListCallback,
            SetRecognitionLabelTextCallback setRecognitionLabelTextCallback,
            SetNameLabelTextCallback setNameLabelTextCallback,
            UpdateListeningUICallback updateListeningUICallback)
        {
            invokeDispatcher = invokeDispatcherCallback;
            closeMainWindow = closeMainWindowCallback;
            addCommandToList = addCommandToListCallback;
            setRecognitionLabelText = setRecognitionLabelTextCallback;
            setNameLabelText = setNameLabelTextCallback;
            updateListeningUI = updateListeningUICallback;

            offlineRecognizer = new SpeechRecognitionEngine();
            offlineRecognizer.SpeechRecognized += OfflineRecognizer_SpeechRecognized;
            offlineRecognizer.SetInputToDefaultAudioDevice();
            SetAssistantName(DEFAULT_ASSISTANT_NAME);
            ActivateOfflineRecognizer();

            cmdList = new VoiceCommandList(x => MessageBox.Show("Unrecognized command: " + x));

            RegisterCommand("{ change [your] { name | activation [command] } | rename yourself } to *", ActionRename);
            RegisterCommand("close { yourself | the assistant }", x => ActionClose());
            RegisterCommand("{ type | write } *", ActionType);
            RegisterCommand("search [for] *", ActionSearch);
            RegisterCommand("open weather forecast", x => ActionOpenWeatherForecast());

            foreach (string dir in new string[] { "documents", "music", "pictures", "videos", "downloads", "desktop" })
            {
                RegisterCommand(ExpressionGenerator.UserDirectory(dir), x => ActionOpenUserDirectory(dir));
            }
        }

        private void OfflineRecognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence > 0.93f)
            {
                Listen();
            }
        }

        private void RegisterCommand(string matchExpression, Action<string> commandAction)
        {
            cmdList.AddCommand(new VoiceCommand(matchExpression, commandAction));
            addCommandToList(matchExpression);
        }

        public async void Listen()
        {
            // Disable the offline speech recognizer
            offlineRecognizer.RecognizeAsyncCancel();
            // Switch the UI to listening mode
            updateListeningUI(true);

            // Let the user know the assistant is listening
            setRecognitionLabelText("Listening...");
            Speaker.Speak("How can I help you?");

            // Listen and recognize
            await GoogleSpeechRecognizer.StreamingMicRecognizeAsync(
                cmdList.HandleInput,
                x => setRecognitionLabelText(x)
                );

            // Switch the UI back to idle mode
            updateListeningUI(false);
            // Re-enable the offline speech recognizer
            ActivateOfflineRecognizer();
        }

        private void SetAssistantName(string name)
        {
            assistantName = name;

            offlineRecognizer.RecognizeAsyncCancel();
            offlineRecognizer.UnloadAllGrammars();
            offlineRecognizer.LoadGrammar(new Grammar(new Choices(name).ToGrammarBuilder()));

            setNameLabelText("Name: " + name.CapitalizeFirstLetter());
        }

        private void ActivateOfflineRecognizer() => offlineRecognizer.RecognizeAsync(RecognizeMode.Multiple);
    }
}