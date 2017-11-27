using System;
using System.Windows;

namespace Termix
{
    public partial class MainWindow : Window
    {
        private VoiceAssistant assistant;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            assistant = new VoiceAssistant(
                InvokeDispatcher,
                () => InvokeDispatcher(Close),
                x => InvokeDispatcher(() => listBoxCommandList.Items.Add(ExpressionHandler.GetFirstOption(x))),
                x => InvokeDispatcher(() => labelRealtimeRecognition.Content = x),
                x => InvokeDispatcher(() => labelName.Content = x),
                x => InvokeDispatcher(() => UpdateListeningUI(x))
                );
        }

        private void ButtonListen_Click(object sender, RoutedEventArgs e) => assistant.Listen();

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

        private void InvokeDispatcher(Action action)
        {
            Dispatcher?.Invoke(action);
        }
    }
}