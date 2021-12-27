using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class LPILipidParser : ILipidParser {
        public string Target { get; } = "LPI";

        private static readonly TotalChainParser chainsParser = new TotalChainParser(1);
        public static readonly string Pattern = $"LPI\\s*(?<sn>{chainsParser.Pattern})";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 9,
            MassDiffDictionary.HydrogenMass * 18,
            MassDiffDictionary.OxygenMass * 11,
            //MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.LPI, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}
