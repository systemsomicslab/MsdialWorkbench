using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class PGLipidParser : ILipidParser {
        public string Target { get; } = "PG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^PG\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.OxygenMass * 8,
            //MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                if (chains.OxidizedCount > 0)
                {
                    return new Lipid(LbmClass.OxPG, Skelton + chains.Mass, chains);
                }
                return new Lipid(LbmClass.PG, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}
