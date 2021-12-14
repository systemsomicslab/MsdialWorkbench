using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class MGLipidParser : ILipidParser {
        public string Target { get; } = "MG";

        private static readonly TotalChainParser chainsParser = new TotalChainParser(1);
        public static readonly string Pattern = $"MG\\s*(?<sn>{chainsParser.Pattern})";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 7,
            MassDiffDictionary.OxygenMass * 3,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.MG, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}
