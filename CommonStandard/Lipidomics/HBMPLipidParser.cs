using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class HBMPLipidParser : ILipidParser {
        public string Target { get; } = "HBMP";

        private static readonly TotalChainParser chainsParser = new TotalChainParser(3);
        public static readonly string Pattern = $"HBMP\\s*(?<sn>{chainsParser.Pattern})";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 12,
            MassDiffDictionary.OxygenMass * 8,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.HBMP, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}
