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

        public static void GoogleSearch(string searchQuery)
        {
            Windows.OpenURLInWebBrowser("https://www.google.com/search?q=" + System.Web.HttpUtility.UrlEncode(searchQuery));
        }
    }
}
