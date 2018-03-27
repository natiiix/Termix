using System;

namespace Termix
{
    public class DataAlias
    {
        public string[] Alternatives { get; private set; }
        public string Value { get; private set; }

        public DataAlias(string aliasEntry)
        {
            string[] parts = aliasEntry.Split(new char[] { ';' }, 2);

            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid alias entry: " + aliasEntry);
            }

            Alternatives = parts[0].ToLower().Split(',');
            Value = parts[1];
        }

        public bool ContainsAlternative(string alternative, bool caseSensitive = false)
        {
            foreach (string alt in Alternatives)
            {
                if (caseSensitive)
                {
                    if (alt == alternative)
                    {
                        return true;
                    }
                }
                else
                {
                    if (alt.ToLower() == alternative.ToLower())
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
