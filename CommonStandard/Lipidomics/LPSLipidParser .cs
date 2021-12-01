using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class LPSLipidParser : ILipidParser {
        public string Target { get; } = "LPS";

        private static readonly TotalChainParser chainsParser = new TotalChainParser(2);
        public static readonly string Pattern = $"LPS\\s*(?<sn>{chainsParser.Pattern})";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 14,
            MassDiffDictionary.OxygenMass * 7,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.LPS, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}
