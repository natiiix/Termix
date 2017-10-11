using System;

namespace Termix
{
    public class VoiceCommand
    {
        public delegate bool MatchTestFunction(string input);
        public delegate string ExtractParameterCallback(string input);

        private readonly MatchTestFunction MatchTest;
        private readonly ExtractParameterCallback ExtractParameter;
        private readonly Action<string> CommandAction;

        public VoiceCommand(MatchTestFunction inputMatchesCommand, ExtractParameterCallback extractCommandParameter, Action<string> commandAction)
        {
            MatchTest = inputMatchesCommand;
            ExtractParameter = extractCommandParameter;
            CommandAction = commandAction;
        }

        public bool DoActionIfMatch(string input)
        {
            if (MatchTest(input))
            {
                CommandAction(ExtractParameter(input));
                return true;
            }

            return false;
        }
    }
}
