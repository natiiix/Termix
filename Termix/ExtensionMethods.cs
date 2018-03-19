using System;
using System.Linq;

namespace Termix
{
    public static class ExtensionMethods
    {
        public static bool StartsWithCaseInsensitive(this string str, string value) => str.ToLower().StartsWith(value.ToLower());

        public static bool EqualsCaseInsensitive(this string str, string value) => str.ToLower() == value.ToLower();

        public static string[] SplitWords(this string str) => str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        public static string[] Split(this string str, string delimiter, StringSplitOptions options = StringSplitOptions.None) => str.Split(new string[] { delimiter }, options);

        public static string TrimSpaces(this string str) => str.Trim(' ');

        public static string CapitalizeFirstLetter(this string str)
        {
            if (str.Length == 0)
            {
                return string.Empty;
            }

            char firstCharCapital = char.ToUpper(str.First());
            string remainder = str.Substring(1);

            return firstCharCapital + remainder;
        }

        public static int RoundToInt(this double value) => (int)Math.Round(value);
    }
}
