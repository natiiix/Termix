using System;
using System.Text.RegularExpressions;

namespace Termix
{
    public class VoiceCommand
    {
        public readonly AssistantMode Mode;
        private readonly Regex RegExPattern;
        private readonly Action<string[]> CommandAction;

        public VoiceCommand(string regex, Action<string[]> action, AssistantMode mode = AssistantMode.All)
        {
            Mode = mode;
            RegExPattern = new Regex($"^(?:{regex})$", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            CommandAction = action;
        }

        public bool DoActionIfMatch(string input)
        {
            Match match = RegExPattern.Match(input);

            if (match.Success)
            {
                string[] args = new string[match.Groups.Count - 1];

                for (int i = 1; i < match.Groups.Count; i++)
                {
                    Group group = match.Groups[i];
                    args[i - 1] = group.Value;
                }

                CommandAction(args);
            }

            return match.Success;
        }
    }
}
