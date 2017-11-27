using System;
using System.Collections.Generic;
using System.Linq;

namespace Termix
{
    public static class ExpressionHandler
    {
        // Definitions of start and end characters

        private static readonly char[] starts = new char[] { '"', '(', '[', '{' };
        private static readonly char[] ends = new char[] { '"', ')', ']', '}' };

        private const char ALTERNATIVE_SEPARATOR = '|';
        private const char VALUE_INDICATOR = '*';

        private const int PAIR_OPTIONAL = 2;
        private const int PAIR_ALTERNATIVES = 3;

        public static bool Compare(string str, string expression, out string value)
        {
            int valueIdx = expression.IndexOf(VALUE_INDICATOR);

            if (valueIdx >= 0 && valueIdx < expression.Length - 1)
            {
                throw new ArgumentException("Invalid expression! Value indicator must be at the very end of an expression!");
            }

            return RemainderAfterExpression(str.ToLower(), expression.ToLower(), out value) == 0;
        }

        public static string GetFirstOption(string expression)
        {
            // Create list for the output parts
            List<string> firstOptionParts = new List<string>();

            // Split the input expression into parts
            List<string> parts = SplitExpressions(expression.TrimSpaces());

            // Iterate through the parts
            foreach (string part in parts)
            {
                // Ignore optional parts
                if (part.First() == starts[PAIR_OPTIONAL] && part.Last() == ends[PAIR_OPTIONAL])
                {
                    continue;
                }
                // Part with alternatives
                else if (part.First() == starts[PAIR_ALTERNATIVES] && part.Last() == ends[PAIR_ALTERNATIVES])
                {
                    // Get the first alternative
                    string firstAlternative = SplitAlternatives(part.Substring(1, part.Length - 2)).First();

                    // Recursively process the first alternative
                    firstOptionParts.Add(GetFirstOption(firstAlternative));
                }
                else
                {
                    firstOptionParts.Add(part);
                }
            }

            return string.Join(" ", firstOptionParts);
        }

        private static int RemainderAfterExpression(string str, string expr, out string value)
        {
            string remainder = str;
            List<string> exprParts = SplitExpressions(expr.TrimSpaces());

            value = string.Empty;

            foreach (string part in exprParts)
            {
                if (StartsWithExpression(remainder, part, out int matchLength, out value))
                {
                    remainder = remainder.Substring(matchLength).TrimSpaces();
                }
                else
                {
                    return -1;
                }
            }

            return remainder.Length;
        }

        private static int FindEndOfPair(string str, int startIdx = 0)
        {
            // Get the start char
            char cStart = str[startIdx];

            // Invalid start char
            if (!starts.Contains(cStart))
            {
                return -1;
            }

            // Find the end char
            char cEnd = '\0';

            for (int i = 0; i < starts.Length; i++)
            {
                if (cStart == starts[i])
                {
                    cEnd = ends[i];
                    break;
                }
            }

            // Find the end of the pair
            int depth = 0;

            for (int i = startIdx + 1; i < str.Length; i++)
            {
                char c = str[i];

                // End of the pair
                if (depth == 0 && cEnd == c)
                {
                    return i;
                }
                // Start of another pair
                else if (starts.Contains(c))
                {
                    depth++;
                }
                // End of another pair
                else if (ends.Contains(c))
                {
                    depth--;
                }
            }

            return -1;
        }

        private static List<string> SplitExpressions(string str)
        {
            List<string> parts = new List<string>();

            int partStart = 0;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (c == ' ')
                {
                    // Skip leading spaces
                    if (i == partStart)
                    {
                        partStart++;
                    }
                    else
                    {
                        parts.Add(str.Substring(partStart, i - partStart).TrimSpaces());
                        partStart = i + 1;
                    }
                }
                else if (starts.Contains(c))
                {
                    int partEnd = FindEndOfPair(str, i);

                    if (partEnd < 0)
                    {
                        throw new Exception("Unable to find the end of this pair!");
                    }

                    parts.Add(str.Substring(partStart, partEnd + 1 - partStart).TrimSpaces());
                    partStart = partEnd + 1;
                    i = partEnd;
                }
            }

            if (partStart < str.Length)
            {
                parts.Add(str.Substring(partStart).TrimSpaces());
            }

            return parts;
        }

        private static List<string> SplitAlternatives(string str)
        {
            List<string> parts = new List<string>();

            int partStart = 0;
            int depth = 0;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                // Skip leading spaces
                if (i == partStart && c == ' ')
                {
                    partStart++;
                }
                else if (depth == 0 && c == ALTERNATIVE_SEPARATOR)
                {
                    parts.Add(str.Substring(partStart, i - partStart).TrimSpaces());
                    partStart = i + 1;
                }
                else if (starts.Contains(c))
                {
                    depth++;
                }
                else if (ends.Contains(c))
                {
                    depth--;
                }
            }

            if (partStart < str.Length)
            {
                parts.Add(str.Substring(partStart).TrimSpaces());
            }

            return parts;
        }

        private static bool StartsWithExpression(string str, string expr, out int matchLength, out string value)
        {
            // Literal expression
            if (str.StartsWith(expr))
            {
                matchLength = expr.Length;
                value = string.Empty;
                return true;
            }
            // Value expression
            else if (expr == VALUE_INDICATOR.ToString())
            {
                matchLength = str.Length;
                value = str;
                return true;
            }
            // Optional expression
            else if (expr.First() == starts[PAIR_OPTIONAL] &&
                expr.Last() == ends[PAIR_OPTIONAL])
            {
                string partValue = expr.Substring(1, expr.Length - 2).TrimSpaces();

                if (StartsWithExpression(str, partValue, out matchLength, out value))
                {
                    return true;
                }
                else
                {
                    matchLength = 0;
                    value = string.Empty;
                    return true;
                }
            }
            // Expression with alternatives
            else if (expr.First() == starts[PAIR_ALTERNATIVES] &&
                     expr.Last() == ends[PAIR_ALTERNATIVES])
            {
                // Get the alternatives ordered by their length in a descending order
                IEnumerable<string> alternatives = SplitAlternatives(expr.Substring(1, expr.Length - 2)).Select(x => x.TrimSpaces()).OrderByDescending(x => x.Length);

                foreach (string alt in alternatives)
                {
                    if (StartsWithExpression(str, alt, out matchLength, out value))
                    {
                        return true;
                    }
                }
            }
            else if (expr.Any(x => starts.Contains(x) || ends.Contains(x)))
            {
                int rem = RemainderAfterExpression(str, expr, out value);

                if (rem < 0)
                {
                    matchLength = 0;
                    return false;
                }

                matchLength = str.Length - rem;
                return matchLength > 0;
            }

            // String doesn't start with the expression
            matchLength = -1;
            value = string.Empty;
            return false;
        }
    }
}