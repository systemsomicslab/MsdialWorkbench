using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class LPCLipidParser : ILipidParser {
        public string Target { get; } = "LPC";

        private static readonly TotalChainParser chainsParser = new TotalChainParser(2);
        public static readonly string Pattern = $"LPC\\s*(?<sn>{chainsParser.Pattern})";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 8,
            MassDiffDictionary.HydrogenMass * 20,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.LPC, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}
