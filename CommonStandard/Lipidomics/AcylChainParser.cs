using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class AcylChainParser
    {
        public static readonly string Pattern = @"(?<carbon>\d+):(?<db>\d)(\((?<dbpos>\d+(,\d+)*)\))?(;(?<ox>\d*)O)?";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        public IChain Parse(string chainStr) {
            var match = pattern.Match(chainStr);
            if (match.Success) {
                var groups = match.Groups;
                var numCarbon = int.Parse(groups["carbon"].Value);
                var numOx = groups["ox"].Success ? int.TryParse(groups["ox"].Value, out var ox) ? ox : 1 : 0;

                if (groups["dbpos"].Success) {
                    return new AcylChain(numCarbon, DoubleBond.CreateFromPosition(groups["dbpos"].Value.Split(',').Select(p => int.Parse(p)).ToArray()), new Oxidized(numOx));
                }
                else {
                    return new AcylChain(numCarbon, new DoubleBond(int.Parse(groups["db"].Value)), new Oxidized(numOx));
                }
            }
            return null;
        }
    }
}
