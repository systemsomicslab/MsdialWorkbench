using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class AlkylChainParser
    {
        public static readonly string Pattern = @"(?<plasm>[OP])-(?<carbon>\d+):(?<db>\d)(\((?<dbpos>\d+(,\d+)*)\))?(;(?<ox>\d*)O)?";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        public IChain Parse(string chainStr) {
            var match = pattern.Match(chainStr);
            if (match.Success) {
                var groups = match.Groups;
                var numCarbon = int.Parse(groups["carbon"].Value);
                var numOx = !groups["ox"].Success ? 0 : int.TryParse(groups["ox"].Value, out var ox) ? ox : 1;

                if (groups["dbpos"].Success) {
                    switch (groups["plasm"].Value) {
                        case "P":
                            return new SpecificAlkylChain(numCarbon, new[] { 1, }.Concat(groups["dbpos"].Value.Split(',').Select(p => int.Parse(p))).ToList(), numOx);
                        case "O":
                            return new SpecificAlkylChain(numCarbon, groups["dbpos"].Value.Split(',').Select(p => int.Parse(p)).ToList(), numOx);
                    }
                }
                else {
                    switch (groups["plasm"].Value) {
                        case "P":
                            return new PlasmalogenAlkylChain(numCarbon, int.Parse(groups["db"].Value), numOx);
                        case "O":
                            return new AlkylChain(numCarbon, int.Parse(groups["db"].Value), numOx);
                    }
                }
            }
            return null;
        }
    }
}
