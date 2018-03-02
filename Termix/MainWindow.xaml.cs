using System;
using System.Windows;
using System.Globalization;

namespace Termix
{
    public partial class MainWindow : Window
    {
        private VoiceAssistant assistant;

        public MainWindow()
        {
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(0x0409);

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            assistant = new VoiceAssistant(
                InvokeDispatcher,
                () => InvokeDispatcher(Close),
                x => InvokeDispatcher(() => labelRealtimeRecognition.Content = x),
                x => InvokeDispatcher(() => labelName.Content = x),
                x => InvokeDispatcher(() => UpdateListeningUI(x)),
                x => InvokeDispatcher(() => AppendLog(x))
                );
        }

        private void ButtonListen_Click(object sender, RoutedEventArgs e) => assistant.Listen();

        private void TextBoxLog_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => textBoxLog.ScrollToEnd();

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

        private void InvokeDispatcher(Action action) => Dispatcher?.Invoke(action);

        private void AppendLog(string text)
        {
            if (textBoxLog.Text.Length > 0)
            {
                textBoxLog.Text += Environment.NewLine;
            }

            textBoxLog.Text += text;
        }
    }
}
