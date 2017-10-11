using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Termix
{
    public static class ExtensionMethods
    {
        public static bool StartsWithCaseInsensitive(this string str, string value) => str.ToLower().StartsWith(value.ToLower());

        public static bool EqualsCaseInsensitive(this string str, string value) => str.ToLower() == value.ToLower();
    }
}