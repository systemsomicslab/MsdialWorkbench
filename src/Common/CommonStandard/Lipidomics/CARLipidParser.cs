using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class CARLipidParser : ILipidParser {
        public string Target { get; } = "CAR";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^CAR\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 7,
            MassDiffDictionary.HydrogenMass * 14,
            MassDiffDictionary.OxygenMass * 3,
            MassDiffDictionary.NitrogenMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.CAR, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}
