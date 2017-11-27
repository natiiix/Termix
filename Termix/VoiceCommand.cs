using System;

namespace Termix
{
    public class VoiceCommand
    {
        public delegate bool MatchTestFunction(string input);

        public delegate string ExtractParameterCallback(string input);

        private readonly string MatchExpression;
        private readonly Action<string> CommandAction;

        public VoiceCommand(string matchExpression, Action<string> commandAction)
        {
            MatchExpression = matchExpression;
            CommandAction = commandAction;
        }

        public bool DoActionIfMatch(string input)
        {
            if (ExpressionHandler.Compare(input, MatchExpression, out string value))
            {
                CommandAction(value);
                return true;
            }

            return false;
        }
    }
}