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
                            return new AlkylChain(numCarbon, DoubleBond.CreateFromPosition(new[] { 1, }.Concat(groups["dbpos"].Value.Split(',').Select(p => int.Parse(p))).ToArray()), new Oxidized(numOx));
                        case "O":
                            return new AlkylChain(numCarbon, DoubleBond.CreateFromPosition(groups["dbpos"].Value.Split(',').Select(p => int.Parse(p)).ToArray()), new Oxidized(numOx));
                    }
                }
                else {
                    switch (groups["plasm"].Value) {
                        case "P":
                            return new AlkylChain(numCarbon, new DoubleBond(int.Parse(groups["db"].Value) + 1, DoubleBondInfo.Create(1)), new Oxidized(numOx));
                        case "O":
                            return new AlkylChain(numCarbon, new DoubleBond(int.Parse(groups["db"].Value)), new Oxidized(numOx));
                    }
                }
            }
            return null;
        }
    }
}
