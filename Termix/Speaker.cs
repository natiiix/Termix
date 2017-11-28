using System.Speech.Synthesis;

namespace Termix
{
    public static class Speaker
    {
        private static SpeechSynthesizer synthesizer = new SpeechSynthesizer();

        public static void Speak(string textToSpeak) => synthesizer.SpeakAsync(textToSpeak);
    }
}