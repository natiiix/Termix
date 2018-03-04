using System.Collections.Generic;
using System.Linq;

namespace Termix
{
    public static class HelperFunctions
    {
        public static readonly Dictionary<string, int> NumberWords = new Dictionary<string, int>()
        {
            { "zero", 0 },
            { "one", 1 },
            { "two", 2 },
            { "three", 3 },
            { "four", 4 },
            { "five", 5 },
            { "six", 6 },
            { "seven", 7 },
            { "eight", 8 },
            { "nine", 9 },
            { "ten", 10 }
        };

        public static int GetIntFromString(string str)
        {
            if (int.TryParse(str, out int value))
            {
                return value;
            }

            return NumberWords[str];
        }

        public static double GetDoubleFromString(string str)
        {
            if (double.TryParse(str, out double value))
            {
                return value;
            }
            else if (str.EndsWith("%") && double.TryParse(str.Substring(0, str.Length - 1), out double percent))
            {
                return percent / 100d;
            }

            try
            {
                return NumberWords[str];
            }
            catch (KeyNotFoundException)
            {
                return double.NaN;
            }
        }

        public static string GetGoogleSearchURL(string searchQuery) => "https://www.google.com/search?q=" + System.Web.HttpUtility.UrlEncode(searchQuery);

        public static string GetYouTubeSearchURL(string searchQuery) => "https://www.youtube.com/results?search_query=" + System.Web.HttpUtility.UrlEncode(searchQuery);

        public static string GetWikipediaSearchURL(string searchQuery) => "https://en.wikipedia.org/w/index.php?search=" + System.Web.HttpUtility.UrlEncode(searchQuery);

        public static void GoogleSearch(string searchQuery) => Windows.OpenURLInWebBrowser(GetGoogleSearchURL(searchQuery));

        public static void YouTubeSearch(string searchQuery) => Windows.OpenURLInWebBrowser(GetYouTubeSearchURL(searchQuery));

        public static void WikipediaSearch(string searchQuery) => Windows.OpenURLInWebBrowser(GetWikipediaSearchURL(searchQuery));

        public static string GetNonEmptyString(params string[] arr) => arr.Single(x => !string.IsNullOrEmpty(x));
    }
}
