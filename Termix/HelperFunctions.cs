namespace Termix
{
    public static class HelperFunctions
    {
        public static double GetNumberFromString(string str)
        {
            if (double.TryParse(str, out double value))
            {
                return value;
            }
            else if (str.EndsWith("%") && double.TryParse(str.Substring(0, str.Length - 1), out double percent))
            {
                return percent / 100d;
            }

            switch (str)
            {
                case "zero":
                    return 0;

                case "one":
                    return 1;

                case "two":
                    return 2;

                case "three":
                    return 3;

                case "four":
                    return 4;

                case "five":
                    return 5;

                case "six":
                    return 6;

                case "seven":
                    return 7;

                case "eight":
                    return 8;

                case "nine":
                    return 9;

                case "ten":
                    return 10;

                default:
                    break;
            }

            return double.NaN;
        }

        public static string GetGoogleSearchURL(string searchQuery) => "https://www.google.com/search?q=" + System.Web.HttpUtility.UrlEncode(searchQuery);

        public static string GetYouTubeSearchURL(string searchQuery) => "https://www.youtube.com/results?search_query=" + System.Web.HttpUtility.UrlEncode(searchQuery);

        public static string GetWikipediaSearchURL(string searchQuery) => "https://en.wikipedia.org/w/index.php?search=" + System.Web.HttpUtility.UrlEncode(searchQuery);

        public static void GoogleSearch(string searchQuery) => Windows.OpenURLInWebBrowser(GetGoogleSearchURL(searchQuery));

        public static void YouTubeSearch(string searchQuery) => Windows.OpenURLInWebBrowser(GetYouTubeSearchURL(searchQuery));

        public static void WikipediaSearch(string searchQuery) => Windows.OpenURLInWebBrowser(GetWikipediaSearchURL(searchQuery));
    }
}
