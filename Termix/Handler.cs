using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Termix
{
    public class Handler
    {
        public void Handle(string input)
        {
            System.Windows.MessageBox.Show(input);
        }
    }
}
