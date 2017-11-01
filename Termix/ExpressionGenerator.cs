namespace Termix
{
    public static class ExpressionGenerator
    {
        public static string UserDirectory(string dirName) => "open [{ my | the }] " + dirName + " [{ directory | folder | library }]";
    }
}