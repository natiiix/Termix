using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Termix
{
    public class VoiceCommandList
    {
        private readonly Action<string> DefaultAction;
        private List<VoiceCommand> Commands;

        public VoiceCommandList(Action<string> defaultAction)
        {
            DefaultAction = defaultAction;
            Commands = new List<VoiceCommand>();
        }

        public void AddCommand(VoiceCommand cmd)
        {
            Commands.Add(cmd);
        }

        public void HandleInput(string input)
        {
            foreach (VoiceCommand cmd in Commands)
            {
                if (cmd.DoActionIfMatch(input))
                {
                    return;
                }
            }

            DefaultAction(input);
        }
    }
}