using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class AcylChainParser
    {
        public static readonly string Pattern = @"(?<carbon>\d+):(?<db>\d)(\(d(?<dbpos>\d+(,\d+)*)\))?(;(?<ox>\d*)O)?";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        public IAcylChain Parse(string chainStr) {
            var match = pattern.Match(chainStr);
            if (match.Success) {
                var groups = match.Groups;
                var numCarbon = int.Parse(groups["carbon"].Value);
                var numOx = groups["ox"].Success ? int.TryParse(groups["ox"].Value, out var ox) ? ox : 1 : 0;

                if (groups["dbpos"].Success) {
                    return new SpecificAcylChain(numCarbon, groups["dbpos"].Value.Split(',').Select(p => int.Parse(p)).ToList(), numOx);
                }
                else {
                    return new AcylChain(numCarbon, int.Parse(groups["db"].Value), numOx);
                }
            }
            return null;
        }
    }
}
