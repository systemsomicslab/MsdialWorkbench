using CompMs.Common.Parser;

namespace CompareAnnotationResult
{
    internal class Program
    {
        static void Main(string[] args) {
            var data = CommandLineParser.Parse<CommandLineData>(args);
        }
    }
}