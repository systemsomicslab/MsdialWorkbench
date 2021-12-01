using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class PILipidParser : ILipidParser {
        public string Target { get; } = "PI";

        private static readonly TotalChainParser chainsParser = new TotalChainParser(2);
        public static readonly string Pattern = $"PI\\s*(?<sn>{chainsParser.Pattern})";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 9,
            MassDiffDictionary.HydrogenMass * 17,
            MassDiffDictionary.OxygenMass * 11,
            //MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.PI, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}
