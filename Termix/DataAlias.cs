using System;
using System.Text.RegularExpressions;

namespace Termix
{
    public class DataAlias
    {
        public readonly string Pattern;
        public readonly Regex Regex;
        public readonly string Value;

        public DataAlias(string aliasEntry)
        {
            string[] parts = aliasEntry.Split(new char[] { ';' }, 2);

            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid alias entry: " + aliasEntry);
            }

            Pattern = parts[0];
            Regex = new Regex($"^(?:{parts[0]})$", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Value = parts[1];
        }
    }
}
