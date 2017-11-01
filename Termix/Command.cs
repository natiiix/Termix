namespace Termix
{
    public class Command
    {
        private string[] Expressions;

        public Command(params string[] expressions)
        {
            Expressions = expressions;
        }
    }
}